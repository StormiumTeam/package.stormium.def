using package.stormium.def;
using package.stormiumteam.shared.ecs;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterMovementGroup))]
	[UpdateAfter(typeof(CharacterMovementInitSystem))]
	public class StandardDodgeMovementSystem : JobGameBaseSystem
	{
		[ExcludeComponent(typeof(IgnoreCharacterMovement))]
		struct Job : IJobForEachWithEntity_EBCCCC<CharacterPass, CharacterInput, StandardDodgeMovement, Velocity, Stamina>
		{
			public UTick Tick;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<StandardAerialMovement> AerialMovementFromEntity;
			
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<StandardJumpMovement>   JumpMovementFromEntity;

			public void Execute(Entity ent, int i, DynamicBuffer<CharacterPass> passes, ref CharacterInput input, ref StandardDodgeMovement component, ref Velocity vel, ref Stamina stamina)
			{
				if (component.DodgeQueued < Tick)
					return;

				if (!passes.TryGetPass(passes.Length - 1, out var current))
					return;
				
				if ((current.Ground.State & GroundState.StableOnGround) == 0)
					return;
				
				Debug.Log($"Dodge at {Tick}");

				var moveData = current.ToMoveData();
				component.DodgeQueued = default;

				float upForce          = 0.0f; // todo: need to be calculated from ground slope
				var   directionForward = SrtMovement.ComputeDirectionFwd(current.ToWorld.Forward, current.Rotation, input.Move);

				vel.Value = SrtMovement.GroundDodge(vel.Value, directionForward, 0.5f, 14f, 16.5f);

				moveData.Position += vel.normalized * 0.5f;
				PhysicsCharacter.Depenetrate(ref moveData, PhysicsWorld);

				vel.Value.y += 4f + math.max(upForce * 15f, 0);

				if (AerialMovementFromEntity.Exists(ent))
				{
					var aerialUpdater   = AerialMovementFromEntity.GetUpdater(ent);
					var aerialComponent = aerialUpdater.original;
					aerialComponent.AirControl *= 0.5f;

					aerialUpdater.CompareAndUpdate(aerialComponent);
				}

				// If the players jump after the dodge, it need to be treated as a consecutive jump
				if (JumpMovementFromEntity.Exists(ent))
				{
					var jumpUpdater   = JumpMovementFromEntity.GetUpdater(ent);
					var jumpComponent = jumpUpdater.original;
					jumpComponent.IsJumpingInChain = true;

					jumpUpdater.CompareAndUpdate(jumpComponent);
				}

				stamina.Apply(component.StaminaUsage);

				current.Ground.State = GroundState.None;
				current.Position     = moveData.Position;
				current.Velocity     = vel.Value;
				passes.Add(current);
			}
		}

		private LazySystem<BuildPhysicsWorld> m_BuildPhysicWorld;

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				Tick                     = GetTick(true),
				PhysicsWorld = this.L(ref m_BuildPhysicWorld).PhysicsWorld,
				AerialMovementFromEntity = GetComponentDataFromEntity<StandardAerialMovement>(),
				JumpMovementFromEntity   = GetComponentDataFromEntity<StandardJumpMovement>()
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, this.L(ref m_BuildPhysicWorld).FinalJobHandle));
		}
	}
}