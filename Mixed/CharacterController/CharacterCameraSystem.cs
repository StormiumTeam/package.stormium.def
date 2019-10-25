using Revolution.NetCode;
using Stormium.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CharacterController
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RenderInterpolationSystem))]
	public class CharCamera : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((ref Translation tr, ref CameraModifierData camMod, ref CharacterInput charInput, ref AimLookState aimLook, ref Relative<PlayerDescription> playerRelative) =>
			{
				camMod.Position = tr.Value + math.up() * 1.25f;

				var input = charInput;
				if (EntityManager.HasComponent<GamePlayerLocalTag>(playerRelative.Target))
				{
					var local = World.GetExistingSystem<BasicUserCommandUpdateLocal>().Current;

					input.Look = local.Look;
				}

				var aim = new Vector3();
				aim.x = -input.Look.y;
				aim.y = input.Look.x;

				camMod.Rotation = Quaternion.Euler(aim);
			});
		}
	}
}