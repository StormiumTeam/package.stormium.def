using Revolution;
using Revolution.NetCode;
using Revolution.Utils;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Transforms;

namespace DefaultNamespace
{
	public struct RotationSnapshot : IReadWriteSnapshot<RotationSnapshot>, ISynchronizeImpl<Rotation>
	{
		public struct Exclude : IComponentData
		{
			
		}
		
		public QuantizedFloat4 Value;

		public void WriteTo(DataStreamWriter writer, ref RotationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i != 4; i++)
				writer.WritePackedIntDelta(Value[i], baseline.Value[i], compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RotationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i != 4; i++)
				Value[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Value[i], compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in Rotation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.Set(1000, component.Value.value);
		}

		public void SynchronizeTo(ref Rotation component, in DeserializeClientData deserializeData)
		{
			component.Value = Value.Get(0.001f);
		}
		
		public class Synchronize : ComponentSnapshotSystem_Basic<Rotation, RotationSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<Rotation, RotationSnapshot>
		{
		}
	}
}