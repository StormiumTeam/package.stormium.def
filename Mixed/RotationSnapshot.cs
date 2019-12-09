using Revolution;
using Unity.NetCode;
using Revolution.Utils;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Transforms;
using Utilities;

namespace DefaultNamespace
{
	public struct RotationSnapshot : IReadWriteSnapshot<RotationSnapshot>, ISynchronizeImpl<Rotation>
	{
		public struct Exclude : IComponentData
		{

		}

		public CompressedQuaternion Value;

		public void WriteTo(DataStreamWriter writer, ref RotationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedQuaternionDelta(Value, baseline.Value, compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RotationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			Value = reader.ReadPackedQuaternionDelta(ref ctx, baseline.Value, compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in Rotation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.Quaternion = component.Value;
		}

		public void SynchronizeTo(ref Rotation component, in DeserializeClientData deserializeData)
		{
			component.Value = Value.Quaternion;
		}

		public class Synchronize : ComponentSnapshotSystemBasic<Rotation, RotationSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<Rotation, RotationSnapshot>
		{
		}
	}
}