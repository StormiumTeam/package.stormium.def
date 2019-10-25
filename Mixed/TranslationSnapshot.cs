using Revolution;
using Revolution.NetCode;
using Revolution.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace
{
	public struct TranslationSnapshot : IReadWriteSnapshot<TranslationSnapshot>, ISynchronizeImpl<Translation>
	{
		public struct Exclude : IComponentData
		{

		}

		public QuantizedFloat3 Value;

		public void WriteTo(DataStreamWriter writer, ref TranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i != 3; i++)
				writer.WritePackedIntDelta(Value[i], baseline.Value[i], compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i != 3; i++)
				Value[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Value[i], compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.Set(1000, component.Value);
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			component.Value = Value.Get(0.001f);
		}

		public class Synchronize : ComponentSnapshotSystem_Basic<Translation, TranslationSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<Translation, TranslationSnapshot>
		{
		}
	}

	public struct PredictedTranslationSnapshot : IReadWriteSnapshot<PredictedTranslationSnapshot>, ISynchronizeImpl<Translation>, IPredictable<PredictedTranslationSnapshot>
	{
		public struct Exclude : IComponentData
		{

		}

		public struct Use : IComponentData
		{
			
		}

		public QuantizedFloat3 Value;

		public void WriteTo(DataStreamWriter writer, ref PredictedTranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i != 3; i++)
				writer.WritePackedIntDelta(Value[i], baseline.Value[i], compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref PredictedTranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i != 3; i++)
				Value[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Value[i], compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.Set(1000, component.Value);
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			component.Value = Value.Get(0.001f);
		}

		public class Synchronize : ComponentSnapshotSystem_Basic_Predicted<Translation, PredictedTranslationSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);

			public override NativeArray<ComponentType> EntityComponents =>
				new NativeArray<ComponentType>(3, Allocator.TempJob)
				{
					[0] = typeof(Translation),
					[1] = typeof(Use),
					[2] = typeof(TranslationSnapshot.Exclude)
				};
		}

		public class Update : ComponentUpdateSystemInterpolated<Translation, PredictedTranslationSnapshot>
		{
			protected override bool IsPredicted => true;
		}

		public void Interpolate(PredictedTranslationSnapshot target, float factor)
		{
			Value.Result = (int3) math.lerp(Value.Result, target.Value.Result, factor);
		}

		public void PredictDelta(uint tick, ref PredictedTranslationSnapshot baseline1, ref PredictedTranslationSnapshot baseline2)
		{
			var predictor = new GhostDeltaPredictor(tick, Tick, baseline1.Tick, baseline2.Tick);
			for (var i = 0; i != 3; i++)
			{
				Value[i] = predictor.PredictInt(Value[i], baseline1.Value[i], baseline2.Value[i]);
			}
		}
	}
}