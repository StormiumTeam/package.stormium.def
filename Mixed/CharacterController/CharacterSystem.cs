using System;
using package.stormium.def;
using package.stormiumteam.shared.ecs;
using Revolution.NetCode;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterInteractionGroup))]
	[UpdateInWorld(WorldType.ServerWorld)]
	public unsafe partial class CharacterSystem : JobGameBaseSystem
	{
		private partial struct UpdateJob : IJobForEachWithEntity<PhysicsCharacter, PhysicsCollider, Translation, Rotation, Velocity>
		{
			private const int MaxIteration = 8;

			[ReadOnly]
			public UTick Tick;

			[ReadOnly]
			public ComponentDataFromEntity<CharacterInput> InputFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Stamina>                        StaminaFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<SrtGroundMovementComponent>     GroundComponentFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<SrtAerialMovementComponent>     AerialComponentFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<AirTime>     AirTimeFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<SrtJumpMovementComponent>       JumpComponentFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<SrtDodgeMovementComponent>      DodgeComponentFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<SrtWallBounceMovementComponent> WallBounceComponentFromEntity;
			
			[ReadOnly] public ComponentDataFromEntity<Velocity> VelocityFromEntity;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld;

			public float DeltaTime;

			public void Execute(Entity entity, int ent_index, ref PhysicsCharacter physChar, ref PhysicsCollider physColl, ref Translation translation, ref Rotation rotation, ref Velocity velocity)
			{
				var gravity = new float3(0, -15, 0.0f);
				var startVelocity = velocity.Value;

				if (physColl.ColliderPtr->Type != ColliderType.Capsule)
					throw new InvalidOperationException("Only Capsule colliders are allowed for Character Movement.");

				var capsuleColl = (CapsuleCollider*) physColl.ColliderPtr;
				if (physChar.MaxStepHeight > capsuleColl->Radius)
					throw new InvalidOperationException("1");

				var probeColl = SphereCollider.Create(new SphereGeometry
				{
					Radius = physChar.MaxStepHeight
				}, physColl.ColliderPtr->Filter);

				var stamina             = StaminaFromEntity.TryGet(entity, out var hasStaminaComponent, default);
				var groundComponent     = GroundComponentFromEntity.TryGet(entity, out var hasGroundComponent, new SrtGroundMovementComponent {Settings = SrtGroundSettings.NewBase()});
				var aerialComponent     = AerialComponentFromEntity.TryGet(entity, out var hasAerialComponent, new SrtAerialMovementComponent {Settings = SrtAerialSettings.NewBase()});
				var airTime = AirTimeFromEntity.TryGet(entity, out var hasAirTimeComponent, default);
				var jumpComponent       = JumpComponentFromEntity.TryGet(entity, out var hasJumpComponent, default);
				var dodgeComponent      = DodgeComponentFromEntity.TryGet(entity, out var hasDodgeComponent, default);
				var wallBounceComponent = WallBounceComponentFromEntity.TryGet(entity, out var hasWallBounceComponent, default);

				LocalToWorld localToWorld;
				localToWorld.Value = new float4x4(rotation.Value, translation.Value);

				MoveData moveData;
				moveData.Character = physChar;
				moveData.Probe     = (SphereCollider*) probeColl.GetUnsafePtr();
				moveData.Collider  = capsuleColl;
				moveData.Position  = translation.Value;
				moveData.Rotation  = rotation.Value;
				moveData.Velocity  = velocity.Value;

				var groundResult = PhysicsCharacter.CheckGround(moveData, PhysicsWorld);
				var becameUnsupported = false;
				if (groundResult.State == GroundState.StableOnGround && velocity.Value.y > 0.01f)
				{
					groundResult.State = GroundState.None;
					becameUnsupported = true;
				}

				var input = InputFromEntity[entity];

				rotation.Value = quaternion.AxisAngle(math.up(), math.radians(input.Look.x));

				var direction        = SrtMovement.ComputeDirection(rotation.Value, input.Move);
				var directionForward = SrtMovement.ComputeDirectionFwd(localToWorld.Forward, rotation.Value, input.Move);
				if (groundResult.State == GroundState.StableOnGround)
				{
					airTime.Value = math.min(airTime.Value, 0) - DeltaTime;
					
					// ------------------- ------------------- -------------------
					// Jump from ground
					if (hasJumpComponent && jumpComponent.JumpQueued >= Tick || input.Jump)
					{
						jumpComponent.JumpQueued = default;
						
						var strafeAngle = SrtMovement.GetStrafeAngleNormalized(direction, math.float3(velocity.Value.x, 0, velocity.Value.z));
						if (jumpComponent.IsJumpingInChain)
						{
							strafeAngle *= 0.25f;
						}

						velocity.Value   += direction * (strafeAngle * 1.0f);
						velocity.Value.y += jumpComponent.IsJumpingInChain ? 4f : 6f;

						stamina.Apply(jumpComponent.IsJumpingInChain ? jumpComponent.StaminaUsageOnChainingJump : jumpComponent.StaminaUsageOnStandardJump);

						jumpComponent.IsJumpingInChain = true;
					}
					// ------------------- ------------------- -------------------
					// Dodge on ground
					else if (hasDodgeComponent && airTime.Value < -0.1f && dodgeComponent.DodgeQueued >= Tick && stamina.HasEnough(dodgeComponent.StaminaUsage))
					{
						dodgeComponent.DodgeQueued = default;
						
						float upForce = 0.0f;

						velocity.Value = SrtMovement.GroundDodge(velocity.Value, directionForward, 0.5f, 14f, 16.5f);

						moveData.Position += velocity.Value * 0.25f;
						PhysicsCharacter.Depenetrate(ref moveData, PhysicsWorld);

						velocity.Value.y           +=  4f + math.max(upForce * 15f, 0);
						aerialComponent.AirControl *= 0.5f;

						// If the players jump after the dodge, it need to be treated as a consecutive jump
						jumpComponent.IsJumpingInChain = true;

						stamina.Apply(dodgeComponent.StaminaUsage);
					}
					// ------------------- ------------------- -------------------
					// Basic ground movement
					else
					{
						var settings = groundComponent.Settings;
						if (input.Crouch)
						{
							settings.BaseSpeed *= 0.5f;
							settings.SprintSpeed = settings.BaseSpeed;
							settings.SurfaceFriction = 20f;
							settings.FrictionSpeed = settings.BaseSpeed * 0.5f;
							
							// gain a bit more stamina when crouching
							stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * DeltaTime * 0.75f, 0, math.max(stamina.Value, stamina.Max));
						}
						else
						{
							settings.FrictionSpeed = settings.BaseSpeed + 0.1f;
						}
						
						velocity.Value             = SrtMovement.GroundMove(velocity.Value, input.Move, direction, settings, DeltaTime);
						aerialComponent.AirControl = 1.0f;
						
						jumpComponent.IsJumpingInChain = false;	
					}
				}
				else
				{
					airTime.Value = math.max(airTime.Value, 0) + DeltaTime;
					
					// ------------------- ------------------- -------------------
					// Wall Bouncing
					if (hasWallBounceComponent && airTime.Value > 0.1f && CanWallBounce(ref moveData, jumpComponent, dodgeComponent, wallBounceComponent, direction, out var closestHit))
					{
						if (jumpComponent.JumpQueued >= Tick && wallBounceComponent.EnableWallJump)
						{
							jumpComponent.JumpQueued = default;
							
							var bouncePower = 6f;
							if (!stamina.HasEnough(wallBounceComponent.StaminaUsageOnWallJump, out var neededPercentage))
							{
								bouncePower *= neededPercentage;
							}
							
							var bounce = closestHit.SurfaceNormal * bouncePower;
							var verticalBonus = math.distance(math.min(velocity.Value.y, 0), math.min(velocity.Value.y + 4f, 0));
							bounce.y += bouncePower + verticalBonus;

							velocity.Value =  RayUtility.SlideVelocityNoYChange(velocity.Value, closestHit.SurfaceNormal);
							velocity.Value += bounce;

							aerialComponent.AirControl *= 0.5f;

							stamina.Apply(wallBounceComponent.StaminaUsageOnWallJump);
						}
						else if (dodgeComponent.DodgeQueued >= Tick && wallBounceComponent.EnableWallDodge)
						{
							dodgeComponent.DodgeQueued = default;
							
							var power = 1f;
							if (!stamina.HasEnough(wallBounceComponent.StaminaUsageOnWallDodge, out var neededPercentage))
							{
								power *= neededPercentage;
							}
							
							var oldY       = velocity.Value.y;
							var dirInertia = RayUtility.SlideVelocityNoYChange(math.normalizesafe(velocity.xfz), closestHit.SurfaceNormal);
							var speed      = math.clamp(math.length(velocity.Value.xz) + 2f, 12f, 16f);
							
							var choice0 = closestHit.SurfaceNormal;

							var dotProduct = math.dot(directionForward, dirInertia); 
							if (dotProduct < 0)
							{
								directionForward = SrtMovement.ComputeDirectionFwd(localToWorld.Forward, rotation.Value, input.Move * -1);
							}

							var wantedVelocity = (float3) Vector3.Reflect(directionForward, choice0) * speed;
							wantedVelocity += closestHit.SurfaceNormal * math.abs(dotProduct) * 2.5f;
							wantedVelocity.y = math.max(oldY + 2f, 2.5f);

							velocity.Value = math.lerp(velocity.Value, wantedVelocity, power);
							
							moveData.Position += velocity.Value * 0.1f;
							PhysicsCharacter.Depenetrate(ref moveData, PhysicsWorld);

							aerialComponent.AirControl *= 0.1f;

							stamina.Apply(wallBounceComponent.StaminaUsageOnWallDodge);
						}

						wallBounceComponent.LastBounceTick = Tick;
					}
					// ------------------- ------------------- -------------------
					// Normal aerial movement
					else
					{
						aerialComponent.AirControl       = math.max(aerialComponent.AirControl - DeltaTime * 0.1f, 0);
						aerialComponent.Settings.Control = math.clamp(40f * aerialComponent.AirControl, 5f, 100);

						velocity.Value = SrtMovement.AerialMove(velocity.Value, direction, aerialComponent.Settings, DeltaTime);

						// glide in air
						if (airTime.Value > 0.5f && input.Jump)
						{
							stamina.HasEnough(StaminaUsage.FromAbsolute(velocity.speed * 0.05f), out var power);
							if (velocity.Value.y < -0.5f)
							{
								velocity.Value.y = math.lerp(velocity.Value.y, -0.5f, DeltaTime * power * 0.33f);
								velocity.Value.y = Mathf.MoveTowards(velocity.Value.y, -1, DeltaTime * power * 12.5f);
							}

							var flatVel = velocity.Value;
							flatVel.y = 0.0f;
							var dirToUse = direction;
							flatVel   = Vector3.MoveTowards(flatVel, dirToUse * math.length(flatVel + dirToUse * 0.125f), DeltaTime * power * 12.5f);
							flatVel.y = velocity.Value.y;
							var distance = math.distance(flatVel, velocity.Value);
							velocity.Value = flatVel;

							stamina.Apply(StaminaUsage.FromPercentage(math.clamp((power + distance * 2f) * 0.01f, 0.01f, 0.1f)));
						}
						else if (aerialComponent.Drag > 0.0f)
						{
							velocity.Value.x = math.lerp(velocity.Value.x, 0, DeltaTime * aerialComponent.Drag);
							velocity.Value.z = math.lerp(velocity.Value.z, 0, DeltaTime * aerialComponent.Drag);
						}
					}
				}

				var maxStamina = math.max(stamina.Value, stamina.Max);
				stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * DeltaTime, 0, maxStamina);

				StaminaFromEntity.TrySet(entity, stamina, false); // this component is always updated, no matter what
				GroundComponentFromEntity.TrySet(entity, groundComponent, true);
				JumpComponentFromEntity.TrySet(entity, jumpComponent, true);
				AerialComponentFromEntity.TrySet(entity, aerialComponent, false); // this component is always updated, no matter what
				AirTimeFromEntity.TrySet(entity, airTime, false); // this component is always updated, no matter what
				DodgeComponentFromEntity.TrySet(entity, dodgeComponent, true);
				WallBounceComponentFromEntity.TrySet(entity, wallBounceComponent, true);

				// get supported velocity
				var supportedVelocity = float3.zero;
				if ((groundResult.State & GroundState.StableOnGround) != 0)
				{
					var supportedEntity = PhysicsWorld.Bodies[groundResult.RigidBodyIndex].Entity;
					if (VelocityFromEntity.Exists(supportedEntity))
					{
						supportedVelocity = VelocityFromEntity[supportedEntity].Value;
					}
				}

				if (becameUnsupported || velocity.Value.y > 0)
				{
					velocity.Value += supportedVelocity;
					supportedVelocity = float3.zero;
				}
				
				moveData.Position = translation.Value;
				moveData.Velocity = (velocity.Value + supportedVelocity) * DeltaTime;

				var moveEvents = new NativeList<MoveEvent>(16, Allocator.Temp);
				var moveResult = PhysicsCharacter.Move(moveData, PhysicsWorld, moveEvents);

				var moveVector = math.normalizesafe(moveResult.NewPosition - moveData.Position);
				if (velocity.Value.y <= 0 && velocity.speedSqr > 0 && moveVector.y <= 0 && (groundResult.State & GroundState.TouchGround) != 0 && (moveResult.GroundStatus.State & GroundState.TouchGround) == 0)
				{
					moveData.Position = moveResult.NewPosition;

					if (PhysicsCharacter.StickOnGround(ref moveData, in PhysicsWorld, gravity * DeltaTime))
					{
						moveResult.NewPosition = moveData.Position;
					}
				}

				for (var i = 0; i < moveEvents.Length; i++)
				{
					var ev = moveEvents[i];
					if (ev.Type == MoveEventType.Obstacle)
					{
						velocity.Value = RayUtility.SlideVelocityNoYChange(velocity.Value, ev.SurfaceNormal);
					}
				}

				translation.Value = moveResult.NewPosition;
				if (moveResult.GroundStatus.State != GroundState.StableOnGround)
					velocity.Value += gravity * DeltaTime;
				else
					velocity.Value.y = math.max(0.0f, velocity.Value.y);
				
				probeColl.Dispose();
			}
		}

		private BuildPhysicsWorld m_BuildPhysicsWorld;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new UpdateJob
			{
				Tick = GetTick(true),

				InputFromEntity = GetComponentDataFromEntity<CharacterInput>(true),

				StaminaFromEntity             = GetComponentDataFromEntity<Stamina>(),
				GroundComponentFromEntity     = GetComponentDataFromEntity<SrtGroundMovementComponent>(),
				AerialComponentFromEntity     = GetComponentDataFromEntity<SrtAerialMovementComponent>(),
				AirTimeFromEntity= GetComponentDataFromEntity<AirTime>(),
				JumpComponentFromEntity       = GetComponentDataFromEntity<SrtJumpMovementComponent>(),
				DodgeComponentFromEntity      = GetComponentDataFromEntity<SrtDodgeMovementComponent>(),
				WallBounceComponentFromEntity = GetComponentDataFromEntity<SrtWallBounceMovementComponent>(),
				
				VelocityFromEntity = GetComponentDataFromEntity<Velocity>(true),

				PhysicsWorld = m_BuildPhysicsWorld.PhysicsWorld,
				DeltaTime    = World.GetExistingSystem<ServerSimulationSystemGroup>().UpdateDeltaTime
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_BuildPhysicsWorld.FinalJobHandle));
		}
	}
}