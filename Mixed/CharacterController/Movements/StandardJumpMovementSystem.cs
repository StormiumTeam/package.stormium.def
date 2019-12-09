using package.stormium.def;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterMovementGroup))]
	[UpdateAfter(typeof(CharacterMovementInitSystem))]
	public class StandardJumpMovementSystem : JobGameBaseSystem
	{
		[BurstCompile]
		[ExcludeComponent(typeof(IgnoreCharacterMovement))]
		struct Job : IJobForEach_BCCCC<CharacterPass, CharacterInput, StandardJumpMovement, Velocity, Stamina>
		{
			public UTick Tick;

			public void Execute(DynamicBuffer<CharacterPass> passes, ref CharacterInput input, ref StandardJumpMovement component, ref Velocity vel, ref Stamina stamina)
			{
				if (UTick.AddMsNextFrame(component.LastJump, 100) >= Tick || !(component.JumpQueued >= Tick || input.Jump))
					return;

				if (!passes.TryGetPass(passes.Length - 1, out var current))
					return;
				
				if ((current.Ground.State & GroundState.StableOnGround) == 0)
					return;

				component.JumpQueued = default;
				component.LastJump = Tick;

				var strafeAngleNormalized = SrtMovement.GetStrafeAngleNormalized(current.Direction, math.float3(vel.Value.x, 0, vel.Value.z));
				var strafeAngle = strafeAngleNormalized * 2.25f;
				if (component.IsJumpingInChain)
				{
					strafeAngle *= 0.325f;
				}

				vel.Value   += current.Direction * (strafeAngle * 1.0f);
				vel.Value.y = math.max(0, component.IsJumpingInChain ? 4f : 6f);

				if (component.IsJumpingInChain)
				{
					stamina.Apply(component.StaminaUsageOnChainingJump);
					stamina.Apply(StaminaUsage.FromAbsolute(strafeAngleNormalized * 0.1f));
				}
				else
				{
					stamina.Apply(component.StaminaUsageOnStandardJump);
				}

				component.IsJumpingInChain = true;

				current.Ground.State = GroundState.None;
				current.Velocity     = vel.Value;
				passes.Add(current);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				Tick = GetTick(true)
			}.Schedule(this, inputDeps);
		}
	}
}