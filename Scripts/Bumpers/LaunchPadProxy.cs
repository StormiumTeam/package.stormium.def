using Unity.Entities;
using Unity.Mathematics;

namespace Scripts.Bumpers
{
	public class LaunchPadProxy : ComponentDataProxy<LaunchPad>
	{
		protected override void ValidateSerializedData(ref LaunchPad serializedData)
		{
			base.ValidateSerializedData(ref serializedData);

			serializedData.momentum  = math.clamp(serializedData.momentum, 0, 1);
		}
	}
}