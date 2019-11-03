using package.stormium.def;
using Revolution.NetCode;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace CharacterController
{
	[UpdateInGroup(typeof(PredictionSimulationSystemGroup))]
	[UpdateInWorld(WorldType.ServerWorld)]
	public class CharacterGroundMovement : JobComponentSystem
	{
		public struct Job : IJobForEach_BCCCC<CharacterPass, SrtGroundMovementComponent, Velocity, Translation, AimLookState>
		{
			public float DeltaTime;

			public void Execute(DynamicBuffer<CharacterPass> passes, ref SrtGroundMovementComponent component, ref Velocity vel, ref Translation pos, ref AimLookState look)
			{
				var current = passes[passes.Length - 1];
				if ((current.Ground.State & GroundState.StableOnGround) == 0)
					return;

				vel.Value = SrtMovement.GroundMove(vel.Value, look.Aim, current.Direction, component.Settings, DeltaTime);

				current.Velocity = vel.Value;
				passes.Add(current);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				DeltaTime = World.GetExistingSystem<PredictionSimulationSystemGroup>().DeltaTime
			}.Schedule(this, inputDeps);
		}
	}
}