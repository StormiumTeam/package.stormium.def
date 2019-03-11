using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Scripts.Bumpers
{
	public struct LaunchPad : IComponentData
	{
		public float3 Direction;
		public float Force;
	}

	public class LaunchPadSystem : GameBaseSystem
	{
		private struct Static_ForEachData
		{
			public Collider   PadCollider;
			public Vector3    PadPosition;
			public Quaternion PadRotation;
		}

		private static Static_ForEachData s_ForEachData;

		protected override void OnUpdate()
		{
			ForEach((Entity entity, Transform transform, ref LaunchPad launchPad) =>
			{
				Debug.Assert(transform.GetComponent<Collider>());

				var collider = transform.GetComponent<Collider>();

				s_ForEachData.PadCollider = collider;

				ForEach((Entity otherEntity, Transform otherTransform) =>
				{
					if (!otherTransform.GetComponent<Collider>())
						return;

					var otherCollider = otherTransform.GetComponent<Collider>();
					if (!Physics.ComputePenetration(s_ForEachData.PadCollider, s_ForEachData.PadPosition, s_ForEachData.PadRotation,
						otherCollider, otherTransform.position, otherTransform.rotation,
						out _, out _))
						return;
					
					// Create Bump event
				});
			});
		}
	}
}