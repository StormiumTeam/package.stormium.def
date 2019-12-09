using package.stormiumteam.shared.ecs;
using Revolution;
using Unity.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace
{
	public class SupportPadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public Vector3 Velocity;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new Velocity());
			dstManager.AddComponentData(entity, new SupportPad {TargetVelocity = Velocity});
			dstManager.SetOrAddComponentData(entity, new Translation());
		}
	}

	public struct SupportPad : IComponentData
	{
		public float3 TargetVelocity;
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	public class PadSystem : ComponentSystem
	{
		private bool m_Toggle = false;

		protected override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.O))
				m_Toggle = !m_Toggle;

			Entities.ForEach((ref SupportPad pad, ref Translation translation, ref Velocity velocity) =>
			{
				if (m_Toggle)
				{
					translation.Value += velocity.Value * Time.DeltaTime;
					velocity.Value    =  pad.TargetVelocity;
				}
				else
				{
					velocity.Value = float3.zero;
				}

				Debug.DrawRay(translation.Value, Vector3.up, Color.green, 0.02f);

				if (Input.GetKeyDown(KeyCode.R))
					translation.Value = float3.zero;
				if (Input.GetKeyDown(KeyCode.P))
					velocity.Value = -velocity.Value;
			});
		}
	}
}