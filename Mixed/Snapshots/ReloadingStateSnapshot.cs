using Revolution;
using Unity.NetCode;
using Stormium.Default.Mixed;
using StormiumTeam.GameBase.Snapshots;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

[assembly: RegisterGenericComponentType(typeof(Predicted<ReloadingStateSnapshot>))]

namespace StormiumTeam.GameBase.Snapshots
{
	public struct ReloadingStateSnapshot : IReadWriteSnapshot<ReloadingStateSnapshot>, ISynchronizeImpl<ReloadingState>, IPredictable<ReloadingStateSnapshot>
	{
		public struct Exclude : IComponentData
		{}
		
		public int  Progress;
		public uint TimeToReload;

		public void WriteTo(DataStreamWriter writer, ref ReloadingStateSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedIntDelta(Progress, baseline.Progress, compressionModel);
			writer.WritePackedUIntDelta(TimeToReload, baseline.TimeToReload, compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref ReloadingStateSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			Progress     = reader.ReadPackedIntDelta(ref ctx, baseline.Progress, compressionModel);
			TimeToReload = reader.ReadPackedUIntDelta(ref ctx, baseline.TimeToReload, compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in ReloadingState component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Progress     = component.Progress.Value;
			TimeToReload = (uint) component.TimeToReload;

			if (!component.Active)
				Progress = -1;
		}
		
		public void SynchronizeTo(ref ReloadingState component, in DeserializeClientData deserializeData)
		{
			component.Progress.Value = Progress;
			component.TimeToReload   = (int) TimeToReload;
			component.Active         = Progress >= 0;
		}

		public class Synchronize : ComponentSnapshotSystemBasicPredicted<ReloadingState, ReloadingStateSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemInterpolated<ReloadingState, ReloadingStateSnapshot>
		{
			public Update() : base(true)
			{}
		}

		public void Interpolate(ReloadingStateSnapshot target, float factor)
		{
			Progress = (int) math.lerp(Progress, target.Progress, factor);
		}

		public void PredictDelta(uint tick, ref ReloadingStateSnapshot baseline1, ref ReloadingStateSnapshot baseline2)
		{
			var predictor = new GhostDeltaPredictor(tick, this.Tick, baseline1.Tick, baseline2.Tick);
			Progress = predictor.PredictInt(Progress, baseline1.Progress, baseline2.Progress);
		}
	}
}