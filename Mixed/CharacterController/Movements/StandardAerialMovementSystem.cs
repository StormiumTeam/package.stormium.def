using package.stormium.def;
using package.stormiumteam.shared.ecs;
using Unity.NetCode;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterMovementGroup))]
	[UpdateAfter(typeof(CharacterMovementInitSystem))]
	public class StandardAerialMovementSystem : JobComponentSystem
	{
		[ExcludeComponent(typeof(IgnoreCharacterMovement))]
		[RequireComponentTag(typeof(AirTime))]
		[BurstCompile]
		public struct Job : IJobForEachWithEntity_EBCCCCC<CharacterPass, CharacterInput, StandardAerialMovement, Velocity, Translation, Stamina>
		{
			public float DeltaTime;

			[ReadOnly]
			public ComponentDataFromEntity<AirTime> AirTimeFromEntity;

			public void Execute(Entity ent, int i, DynamicBuffer<CharacterPass> passes, ref CharacterInput input, ref StandardAerialMovement component, ref Velocity vel, ref Translation pos, ref Stamina stamina)
			{
				if (!passes.TryGetPass(passes.Length - 1, out var current))
					return;

				if ((current.Ground.State & GroundState.StableOnGround) != 0)
					return;

				var moveData = current.ToMoveData();
				var airTime  = AirTimeFromEntity[ent];

				component.AirControl       = math.max(component.AirControl - DeltaTime * 0.1f, 0);
				component.Settings.Control = math.clamp(40f * component.AirControl, 5f, 100);

				vel.Value = SrtMovement.AerialMove(vel.Value, current.Direction, component.Settings, DeltaTime);

				// glide in air
				if (airTime.Value > 0.5f && input.Jump)
				{
					/*stamina.HasEnough(StaminaUsage.FromAbsolute(vel.speed * 0.05f), out var power);
					if (vel.Value.y < -0.5f)
					{
						var prevPower      = power;
						var groundDistance = math.abs(current.Ground.HitPosition.y - PhysicsCharacter.GetBottomPosition(moveData).y);
						if (groundDistance < 10)
						{
							power *= 1 + (1 - groundDistance * (1f / 10f));
						}

						if (groundDistance < 5)
						{
							power *= 1 + (1 - groundDistance * (1f / 5f));
						}

						var prevY = vel.Value.y;
						vel.Value.y = math.lerp(vel.Value.y, -2.5f, DeltaTime * power * 0.25f);
						vel.Value.y = Mathf.MoveTowards(vel.Value.y, -2.5f, DeltaTime * power * 10f);
						if (prevY < -5f)
							prevY = -5f;
						
						power = prevPower;
					}

					var flatVel = vel.Value;
					flatVel.y = 0.0f;
					var dirToUse = current.Direction;
					flatVel   = Vector3.MoveTowards(flatVel, dirToUse * math.length(flatVel + dirToUse * 0.125f), DeltaTime * power * 12.5f);
					flatVel.y = vel.Value.y;
					var distance = math.distance(flatVel, vel.Value);
					vel.Value = flatVel;

					stamina.Apply(StaminaUsage.FromPercentage(math.clamp((power + distance * 2f) * 0.01f, 0.0125f, 0.125f)));*/
				}
				else if (component.Drag > 0.0f)
				{
					vel.Value.x = Mathf.MoveTowards(vel.Value.x, 0, DeltaTime * component.Drag * 3);
					vel.Value.x = math.lerp(vel.Value.x, 0, DeltaTime * component.Drag);
					vel.Value.z = Mathf.MoveTowards(vel.Value.z, 0, DeltaTime * component.Drag * 3);
					vel.Value.z = math.lerp(vel.Value.z, 0, DeltaTime * component.Drag);

					if (input.Crouch && vel.Value.y > 0)
					{
						var prevY = vel.Value.y;

						stamina.HasEnough(StaminaUsage.FromAbsolute(prevY * 0.045f), out var power);
						vel.Value.y =  math.lerp(vel.Value.y, math.min(prevY, 0), DeltaTime * power * 1.25f);
						vel.Value.y -= power * DeltaTime;

						stamina.Apply(StaminaUsage.FromPercentage(math.clamp(prevY * power * 0.0025f, 0.0025f, 0.1f)));
					}
				}

				current.Velocity = vel.Value;
				passes.Add(current);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				DeltaTime         = Time.DeltaTime,
				AirTimeFromEntity = GetComponentDataFromEntity<AirTime>(),
			}.Schedule(this, inputDeps);
		}
	}
}