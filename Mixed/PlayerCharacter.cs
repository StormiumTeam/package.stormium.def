using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace DefaultNamespace
{
	public struct PlayerCharacter : IComponentData
	{
		public Entity Character;

		public struct Snapshot : ISnapshotDelta<Snapshot>, IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<PlayerCharacter, GhostSetup>
		{
			public uint Tick { get; set; }

			public uint CharacterGhostId;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedUIntDelta(CharacterGhostId, baseline.CharacterGhostId, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				CharacterGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.CharacterGhostId, compressionModel);
			}

			public bool DidChange(Snapshot baseline)
			{
				return baseline.CharacterGhostId != CharacterGhostId;
			}

			public void SynchronizeFrom(in PlayerCharacter component, in GhostSetup setup, in SerializeClientData serializeData)
			{
				CharacterGhostId = setup[component.Character];
			}

			public void SynchronizeTo(ref PlayerCharacter component, in DeserializeClientData deserializeData)
			{
				deserializeData.GhostToEntityMap.TryGetValue(CharacterGhostId, out component.Character);
			}
		}

		public class Synchronize : ComponentSnapshotSystem_Delta<PlayerCharacter, Snapshot, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
		
		public struct Exclude : IComponentData
		{}
	}
}