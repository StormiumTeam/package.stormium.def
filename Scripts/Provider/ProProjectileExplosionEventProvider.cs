using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Scripts.Provider
{
	public class ProProjectileExplosionEventProvider : SystemProvider
	{
		struct SerializeCollectionJob : IJob
		{
			public int ModelId;
			
			public SnapshotRuntime Runtime;
			public DataBufferWriter Buffer;

			[ReadOnly]
			public ComponentDataFromEntity<TargetBumpEvent> ExplosionEventFromEntity;
			[ReadOnly]
			public ComponentDataFromEntity<TargetDamageEvent> DamageEventFromEntity;
			
			public void Execute()
			{
				for (var i = 0; i != Runtime.Entities.Length; i++)
				{
					var (source, modelId) = Runtime.Entities[i];
					if (modelId != ModelId)
						continue;

					byte mask       = 0;
					var  maskMarker = Buffer.WriteByte(0);

					if (ExplosionEventFromEntity.Exists(source))
					{
						MainBit.SetBitAt(ref mask, 0, 1);
						ExplosionEventFromEntity[source].Write(ref Buffer, default, Runtime);
					}

					if (DamageEventFromEntity.Exists(source))
					{
						MainBit.SetBitAt(ref mask, 1, 1);
						DamageEventFromEntity[source].Write(ref Buffer, default, Runtime);
					}

					Buffer.WriteByte(mask, maskMarker);
				}
			}
		}

		struct DeserializeCollectionJob : IJob
		{
			public int ModelId;

			public SnapshotRuntime Runtime;
			public DataBufferReader  Buffer;

			public UnsafeAllocation<int> BufferCursor;

			public ComponentDataFromEntity<TargetBumpEvent> ExplosionEventFromEntity;
			public ComponentDataFromEntity<TargetDamageEvent> DamageEventFromEntity;

			public EntityCommandBuffer Ecb;

			public void Execute()
			{
				for (var i = 0; i != Runtime.Entities.Length; i++)
				{
					var (source, modelId) = Runtime.Entities[i];
					if (modelId != ModelId)
						continue;

					var worldEntity = Runtime.EntityToWorld(source);
					var mask        = Buffer.ReadValue<byte>();

					// TargetExplosionEvent
					if (MainBit.GetBitAt(mask, 0) == 1)
					{
						var bumpEvent = new TargetBumpEvent();
						
						bumpEvent.Read(ref Buffer, Runtime.Header.Sender, Runtime);
						
						if (ExplosionEventFromEntity.Exists(worldEntity))
							ExplosionEventFromEntity[worldEntity] = bumpEvent;
						else
							Ecb.AddComponent(worldEntity, bumpEvent);
					}

					// TargetDamageEvent
					if (MainBit.GetBitAt(mask, 1) == 1)
					{
						var damageEvent = new TargetDamageEvent();
						
						damageEvent.Read(ref Buffer, Runtime.Header.Sender, Runtime);
						
						if (DamageEventFromEntity.Exists(worldEntity))
							DamageEventFromEntity[worldEntity] = damageEvent;
						else
							Ecb.AddComponent(worldEntity, damageEvent);
					}
				}

				BufferCursor.Value = Buffer.CurrReadIndex;
			}
		}

		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(GameEvent),
				typeof(TargetBumpEvent),
				typeof(TargetDamageEvent),
				typeof(ExcludeFromDataStreamer)
			};
			excludedComponents = null;
		}

		private EntityArchetype m_SpawnArchetype;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_SpawnArchetype = EntityManager.CreateArchetype
			(
				typeof(ModelIdent),
				typeof(GameEvent),
				typeof(ExcludeFromDataStreamer),
				typeof(EntitySnapshotManualDestroy),
				typeof(GenerateEntitySnapshot)
			);
		}

		public override void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
		{
			new SerializeCollectionJob
			{
				ModelId = GetModelIdent().Id,
				Buffer = data,
				Runtime = snapshotRuntime,
				ExplosionEventFromEntity = GetComponentDataFromEntity<TargetBumpEvent>(),
				DamageEventFromEntity = GetComponentDataFromEntity<TargetDamageEvent>(),
			}.Run();
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{
			using (var tempEcb = new EntityCommandBuffer(Allocator.TempJob))
			using (var readCursor = new UnsafeAllocation<int>(Allocator.TempJob, 1))
			{
				new DeserializeCollectionJob
				{
					ModelId                  = GetModelIdent().Id,
					Buffer                   = data,
					Runtime                  = snapshotRuntime,
					ExplosionEventFromEntity = GetComponentDataFromEntity<TargetBumpEvent>(),
					DamageEventFromEntity    = GetComponentDataFromEntity<TargetDamageEvent>(),
					
					BufferCursor = readCursor,
					Ecb = tempEcb
				}.Run();

				data.CurrReadIndex = readCursor.Value;
				tempEcb.Playback(EntityManager);
			}
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			return EntityManager.CreateEntity(m_SpawnArchetype);
		}

		public override Entity SpawnLocalEntityDelayed(EntityCommandBuffer entityCommandBuffer)
		{
			var e = entityCommandBuffer.CreateEntity(m_SpawnArchetype);
			entityCommandBuffer.SetComponent(e, GetModelIdent());
			return e;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}
	}
}