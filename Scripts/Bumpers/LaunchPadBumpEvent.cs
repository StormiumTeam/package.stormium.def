using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Scripts.Bumpers
{
	public struct LaunchPadBumpEvent : IComponentData
	{

	}

	public class LaunchPadBumpEventProvider : BaseProviderBatch<LaunchPadBumpEventProvider.Create>
	{
		public struct Create
		{
			public TargetBumpEvent data;
		}

		private struct SerializeCollectionJob : IJob
		{
			public int ModelId;

			public SnapshotRuntime  Runtime;
			public DataBufferWriter Buffer;

			[ReadOnly]
			public ComponentDataFromEntity<TargetBumpEvent> BumpEventFromEntity;

			public void Execute()
			{
				for (var i = 0; i != Runtime.Entities.Length; i++)
				{
					var (source, modelId) = Runtime.Entities[i];
					if (modelId != ModelId)
						continue;

					byte mask       = 0;
					var  maskMarker = Buffer.WriteByte(0);

					if (BumpEventFromEntity.Exists(source))
					{
						MainBit.SetBitAt(ref mask, 0, 1);

						BumpEventFromEntity[source].Write(ref Buffer, default, Runtime);
					}

					Buffer.WriteByte(mask, maskMarker);
				}
			}
		}

		private struct DeserializeCollectionJob : IJob
		{
			public int ModelId;

			public SnapshotRuntime                    Runtime;
			public UnsafeAllocation<DataBufferReader> BufferReference;

			public ComponentDataFromEntity<TargetBumpEvent> BumpEventFromEntity;

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

					if (MainBit.GetBitAt(mask, 0) == 1)
					{
						var bumpEvent = new TargetBumpEvent();

						bumpEvent.Read(ref buffer, Runtime.Header.Sender, Runtime);

						if (BumpEventFromEntity.Exists(worldEntity))
							BumpEventFromEntity[worldEntity] = bumpEvent;
						else
							Ecb.AddComponent(worldEntity, bumpEvent);
					}
				}
			}
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<GameEvent>(),
				ComponentType.ReadWrite<TargetBumpEvent>(),
				ComponentType.ReadWrite<LaunchPadBumpEvent>(),
				ComponentType.ReadWrite<ExcludeFromDataStreamer>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>(),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, data.data);
		}
	}
}