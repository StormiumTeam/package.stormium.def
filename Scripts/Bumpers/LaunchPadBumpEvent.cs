using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Scripts.Bumpers
{
	public struct LaunchPadBumpEvent : IComponentData
	{
		
	}
	
	public class LaunchPadBumpEventProvider : SystemProvider
	{
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

			public SnapshotRuntime  Runtime;
			public DataBufferReader Buffer;

			public UnsafeAllocation<int> BufferCursor;

			public ComponentDataFromEntity<TargetBumpEvent>   BumpEventFromEntity;

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

					if (MainBit.GetBitAt(mask, 0) == 1)
					{
						var bumpEvent = new TargetBumpEvent();
						
						bumpEvent.Read(ref Buffer, Runtime.Header.Sender, Runtime);

						if (BumpEventFromEntity.Exists(worldEntity))
							BumpEventFromEntity[worldEntity] = bumpEvent;
						else
							Ecb.AddComponent(worldEntity, bumpEvent);
					}
				}

				BufferCursor.Value = Buffer.CurrReadIndex;
			}
		}

		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<GameEvent>(),
				ComponentType.ReadWrite<TargetBumpEvent>(), 
				ComponentType.ReadWrite<LaunchPadBumpEvent>(), 
				ComponentType.ReadWrite<ExcludeFromDataStreamer>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>(), 
			};
			excludedComponents = null;
		}

		public override void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
		{
			new SerializeCollectionJob
			{
				ModelId = GetModelIdent().Id,

				Runtime = snapshotRuntime,
				Buffer  = data,

				BumpEventFromEntity = GetComponentDataFromEntity<TargetBumpEvent>()
			}.Run();
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{
			using (var bufferCursor = new UnsafeAllocation<int>(Allocator.TempJob, data.CurrReadIndex))
			using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
			{
				new DeserializeCollectionJob
				{
					ModelId = GetModelIdent().Id,

					Runtime = snapshotRuntime,
					Buffer  = data,

					BufferCursor = bufferCursor,

					BumpEventFromEntity = GetComponentDataFromEntity<TargetBumpEvent>(),

					Ecb = ecb
				};

				ecb.Playback(EntityManager);
			}
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			return EntityManager.CreateEntity(EntityComponents);
		}
		
		public override Entity SpawnLocalEntityDelayed(EntityCommandBuffer entityCommandBuffer)
		{
			var e = entityCommandBuffer.CreateEntity(EntityArchetype);
			entityCommandBuffer.SetComponent(e, GetModelIdent());
			return e;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}
	}
}