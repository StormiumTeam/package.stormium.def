using DefaultNamespace;
using package.stormium.def;
using package.stormiumteam.shared.ecs;
using Unity.NetCode;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterMovementGroup))]
	[UpdateAfter(typeof(StandardDodgeMovementSystem))]
	[UpdateAfter(typeof(StandardJumpMovementSystem))]
	public class StandardGroundMovementSystem : JobComponentSystem
	{
		[ExcludeComponent(typeof(IgnoreCharacterMovement))]
		public struct Job : IJobForEachWithEntity_EBCCCCC<CharacterPass, CharacterInput, StandardGroundMovement, Velocity, Translation, GhostPredictedComponent>
		{
			public uint  ServerTick;
			public float DeltaTime;

			[ReadOnly]
			public ComponentDataFromEntity<AirTime> AirTimeFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<StandardAerialMovement> AerialComponentFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<StandardJumpMovement> JumpComponentFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<Stamina> StaminaFromEntity;

			public void Execute(Entity ent, int i, DynamicBuffer<CharacterPass> passes, ref CharacterInput input, ref StandardGroundMovement component, ref Velocity vel, ref Translation pos, ref GhostPredictedComponent predicted)
			{
				if (!GhostPredictionSystemGroup.ShouldPredict(ServerTick, predicted))
					return;

				if (!passes.TryGetPass(passes.Length - 1, out var current))
					return;

				if ((current.Ground.State & GroundState.StableOnGround) == 0 || current.Velocity.y > 0.01f)
					return;
				
				if (AerialComponentFromEntity.Exists(ent))
				{
					var aerialUpdater   = AerialComponentFromEntity.GetUpdater(ent);
					var aerialComponent = aerialUpdater.original;
					aerialComponent.AirControl = 1.0f;

					aerialUpdater.CompareAndUpdate(aerialComponent);
				}

				if (JumpComponentFromEntity.Exists(ent))
				{
					var jumpUpdater   = JumpComponentFromEntity.GetUpdater(ent);
					var jumpComponent = jumpUpdater.original;
					jumpComponent.IsJumpingInChain = false;

					jumpUpdater.CompareAndUpdate(jumpComponent);
				}

				var settings = component.Settings;
				if (input.Crouch)
				{
					settings.BaseSpeed   = 4f;
					settings.SprintSpeed = 4f;
				}

				settings.FrictionSpeed = settings.SprintSpeed + 0.1f;

				vel.Value = SrtMovement.GroundMove(vel.Value, input.Move, current.Direction, settings, DeltaTime, pos.Value);

				// gain a bit more stamina when not running
				if (vel.speed < component.Settings.BaseSpeed
				    && AirTimeFromEntity.Exists(ent) && AirTimeFromEntity[ent].Value <= -0.25f
				    && StaminaFromEntity.Exists(ent))
				{
					var staminaUpdater = StaminaFromEntity.GetUpdater(ent);
					var stamina        = staminaUpdater.original;
					{
						stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * DeltaTime * 0.75f, 0, math.max(stamina.Value, stamina.Max));
						if (input.Crouch)
						{
							stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * DeltaTime * 0.75f, 0, math.max(stamina.Value, stamina.Max));
						}
					}
					staminaUpdater.CompareAndUpdate(stamina);
				}

				current.Velocity = vel.Value;
				passes.Add(current);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				ServerTick                = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick,
				DeltaTime                 = Time.DeltaTime,
				AirTimeFromEntity         = GetComponentDataFromEntity<AirTime>(),
				AerialComponentFromEntity = GetComponentDataFromEntity<StandardAerialMovement>(),
				JumpComponentFromEntity   = GetComponentDataFromEntity<StandardJumpMovement>(),
				StaminaFromEntity         = GetComponentDataFromEntity<Stamina>()
			}.Schedule(this, inputDeps);
		}
	}
}