using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Stormium.Default.Kits.ProKit
{
	public class ProProjectileExplosionEventProvider : SystemProvider<ProProjectileExplosionEventProvider.CreateDelayedData>
	{
		public struct CreateDelayedData
		{
			public bool            HasBumpEvent;
			public TargetBumpEvent BumpEvent;

			public bool              HasDamageEvent;
			public TargetDamageEvent DamageEvent;
		}

		struct SerializeCollectionJob : IJob
		{
			public int ModelId;

			public SnapshotRuntime  Runtime;
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

			public SnapshotRuntime                    Runtime;
			public UnsafeAllocation<DataBufferReader> BufferReference;

			public ComponentDataFromEntity<TargetBumpEvent>   ExplosionEventFromEntity;
			public ComponentDataFromEntity<TargetDamageEvent> DamageEventFromEntity;

			public EntityCommandBuffer Ecb;

			public void Execute()
			{
				ref var buffer = ref BufferReference.AsRef();
				for (var i = 0; i != Runtime.Entities.Length; i++)
				{
					var (source, modelId) = Runtime.Entities[i];
					if (modelId != ModelId)
						continue;

					var worldEntity = Runtime.EntityToWorld(source);
					var mask        = buffer.ReadValue<byte>();

					// TargetExplosionEvent
					if (MainBit.GetBitAt(mask, 0) == 1)
					{
						var bumpEvent = new TargetBumpEvent();

						bumpEvent.Read(ref buffer, Runtime.Header.Sender, Runtime);

						if (ExplosionEventFromEntity.Exists(worldEntity))
							ExplosionEventFromEntity[worldEntity] = bumpEvent;
						else
							Ecb.AddComponent(worldEntity, bumpEvent);
					}

					// TargetDamageEvent
					if (MainBit.GetBitAt(mask, 1) == 1)
					{
						var damageEvent = new TargetDamageEvent();

						damageEvent.Read(ref buffer, Runtime.Header.Sender, Runtime);

						if (DamageEventFromEntity.Exists(worldEntity))
							DamageEventFromEntity[worldEntity] = damageEvent;
						else
							Ecb.AddComponent(worldEntity, damageEvent);
					}
				}
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

		public override void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
		{
			new SerializeCollectionJob
			{
				ModelId                  = GetModelIdent().Id,
				Buffer                   = data,
				Runtime                  = snapshotRuntime,
				ExplosionEventFromEntity = GetComponentDataFromEntity<TargetBumpEvent>(),
				DamageEventFromEntity    = GetComponentDataFromEntity<TargetDamageEvent>(),
			}.Run();
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{
			using (var tempEcb = new EntityCommandBuffer(Allocator.TempJob))
			{
				new DeserializeCollectionJob
				{
					ModelId                  = GetModelIdent().Id,
					BufferReference          = UnsafeAllocation.From(ref data),
					Runtime                  = snapshotRuntime,
					ExplosionEventFromEntity = GetComponentDataFromEntity<TargetBumpEvent>(),
					DamageEventFromEntity    = GetComponentDataFromEntity<TargetDamageEvent>(),

					Ecb = tempEcb
				}.Run();

				tempEcb.Playback(EntityManager);
			}
		}

		public override void SpawnLocalEntityWithArguments(CreateDelayedData data, NativeList<Entity> outputEntities)
		{
			var e = EntityManager.CreateEntity(EntityArchetype);
			if (data.HasBumpEvent)
				EntityManager.AddComponentData(e, data.BumpEvent);
			if (data.HasDamageEvent)
				EntityManager.AddComponentData(e, data.DamageEvent);
			
			outputEntities.Add(e);
		}

		public override Entity SpawnLocalEntityDelayed(EntityCommandBuffer entityCommandBuffer)
		{
			var e = entityCommandBuffer.CreateEntity(EntityArchetype);
			entityCommandBuffer.SetComponent(e, GetModelIdent());
			return e;
		}
	}
}