using Unity.Entities;
using Unity.Mathematics;

namespace Scripts.Bumpers
{
	public class LaunchPadProxy : ComponentDataProxy<LaunchPad>
	{
		protected override void ValidateSerializedData(ref LaunchPad serializedData)
		{
			base.ValidateSerializedData(ref serializedData);

			serializedData.reset = math.clamp(serializedData.reset, 0, 1);
			serializedData.direction = math.normalizesafe(serializedData.direction);
		}
	}
}