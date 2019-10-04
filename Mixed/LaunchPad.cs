using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.Mixed
{
	[Serializable]
	public struct LaunchPad : IComponentData
	{
		public float3 direction;
		public float3 momentum;
		public float  force;
	}
}