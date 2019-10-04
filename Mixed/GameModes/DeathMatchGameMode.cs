using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Stormium.Default.Mixed.GameModes
{
	public struct DeathMatchGameMode : IReadWriteComponentSnapshot<DeathMatchGameMode>, IGameMode
	{
		public struct Exclude : IComponentData
		{
		}

		public int PointsToWin;

		public void WriteTo(DataStreamWriter writer, ref DeathMatchGameMode baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedIntDelta(PointsToWin, baseline.PointsToWin, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref DeathMatchGameMode baseline, DeserializeClientData jobData)
		{
			PointsToWin = reader.ReadPackedIntDelta(ref ctx, baseline.PointsToWin, jobData.NetworkCompressionModel);
		}

		public class Synchronize : MixedComponentSnapshotSystem<DeathMatchGameMode, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public struct DeathMatchPlayer : IReadWriteComponentSnapshot<DeathMatchPlayer>
	{
		public struct Exclude : IComponentData
		{
		}

		public int    Score;

		public void WriteTo(DataStreamWriter writer, ref DeathMatchPlayer baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedIntDelta(Score, baseline.Score, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref DeathMatchPlayer baseline, DeserializeClientData jobData)
		{
			Score = reader.ReadPackedIntDelta(ref ctx, baseline.Score, jobData.NetworkCompressionModel);
		}

		public class Synchronize : MixedComponentSnapshotSystem<DeathMatchGameMode, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}