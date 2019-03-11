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
			public ComponentDataFromEntity<TargetExplosionEvent> ExplosionEventFromEntity;
			[ReadOnly]
			public ComponentDataFromEntity<TargetDamageEvent> DamageEventFromEntity;
			
			public void Execute()
			{
				for (var i = 0; i != Runtime.Entities.Length; i++)
				{
					var entity = Runtime.Entities[i];
					if (entity.ModelId != ModelId)
						continue;

					byte mask       = 0;
					var  maskMarker = Buffer.WriteByte(0);

					if (ExplosionEventFromEntity.Exists(entity.Source))
					{
						MainBit.SetBitAt(ref mask, 0, 1);
						var explosionData = ExplosionEventFromEntity[entity.Source];

						Buffer.WriteValue(explosionData.Position);
						Buffer.WriteValue((half3) explosionData.Direction);
						Buffer.WriteValue((half3) explosionData.Force);

						Buffer.WriteDynamicIntWithMask((ulong) Runtime.GetIndex(explosionData.Victim), (ulong) Runtime.GetIndex(explosionData.Shooter));
					}

					if (DamageEventFromEntity.Exists(entity.Source))
					{
						MainBit.SetBitAt(ref mask, 1, 1);
						var damageData = DamageEventFromEntity[entity.Source];

						Buffer.WriteDynamicIntWithMask((ulong) damageData.DmgValue, (ulong) Runtime.GetIndex(damageData.Victim), (ulong) Runtime.GetIndex(damageData.Shooter));
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

			public NativeArray<int> CurrReadDataCursor;

			public ComponentDataFromEntity<TargetExplosionEvent> ExplosionEventFromEntity;
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
						var position  = Buffer.ReadValue<float3>();
						var direction = Buffer.ReadValue<half3>();
						var force     = Buffer.ReadValue<half3>();

						Buffer.ReadDynIntegerFromMask(out var unsignedVictimIdx, out var unsignedShooterIdx);

						var victim  = Runtime.GetWorldEntityFromGlobal((int) unsignedVictimIdx);
						var shooter = Runtime.GetWorldEntityFromGlobal((int) unsignedShooterIdx);
						
						var targetExplosionEvent =  new TargetExplosionEvent
						{
							Position = position, Direction = direction, Force = force,
							Victim   = victim, Shooter     = shooter
						};
						
						if (ExplosionEventFromEntity.Exists(worldEntity))
							ExplosionEventFromEntity[worldEntity] = targetExplosionEvent;
						else
							Ecb.AddComponent(worldEntity, targetExplosionEvent);
					}

					// TargetDamageEvent
					if (MainBit.GetBitAt(mask, 1) == 1)
					{
						Buffer.ReadDynIntegerFromMask(out var unsignedDmgValue, out var unsignedVictimIdx, out var unsignedShooterIdx);

						var dmgValue = (int) unsignedDmgValue;
						var victim   = Runtime.GetWorldEntityFromGlobal((int) unsignedVictimIdx);
						var shooter  = Runtime.GetWorldEntityFromGlobal((int) unsignedShooterIdx);

						var targetDamageEvent = new TargetDamageEvent
						{
							DmgValue = dmgValue,
							Victim   = victim, Shooter = shooter
						};

						if (DamageEventFromEntity.Exists(worldEntity))
							DamageEventFromEntity[worldEntity] = targetDamageEvent;
						else
							Ecb.AddComponent(worldEntity, targetDamageEvent);
					}
				}

				CurrReadDataCursor[0] = Buffer.CurrReadIndex;
			}
		}

		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(GameEvent),
				typeof(TargetExplosionEvent),
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
				ExplosionEventFromEntity = GetComponentDataFromEntity<TargetExplosionEvent>(),
				DamageEventFromEntity = GetComponentDataFromEntity<TargetDamageEvent>(),
			}.Run();
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{
			using (var tempEcb = new EntityCommandBuffer(Allocator.TempJob))
			using (var readCursor = new NativeArray<int>(1, Allocator.TempJob) {[0] = data.CurrReadIndex})
			{
				new DeserializeCollectionJob
				{
					ModelId                  = GetModelIdent().Id,
					Buffer                   = data,
					Runtime                  = snapshotRuntime,
					ExplosionEventFromEntity = GetComponentDataFromEntity<TargetExplosionEvent>(),
					DamageEventFromEntity    = GetComponentDataFromEntity<TargetDamageEvent>(),
					
					CurrReadDataCursor = readCursor,
					Ecb = tempEcb
				}.Run();

				data.CurrReadIndex = readCursor[0];
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