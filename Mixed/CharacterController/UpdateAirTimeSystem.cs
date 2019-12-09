using Stormium.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterInteractionGroup))]
	[UpdateAfter(typeof(CharacterMovementInitSystem))]
	[UpdateBefore(typeof(CharacterMovementGroup))]
	public class UpdateAirTimeSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobForEach_BC<CharacterPass, AirTime>
		{
			[ReadOnly]
			public UTick Tick;

			public void Execute(DynamicBuffer<CharacterPass> passes, ref AirTime airTime)
			{
				if (!passes.TryGetPass(0, out var current))
					return;
				
				if (current.Ground.State == GroundState.StableOnGround)
					airTime.Value = math.min(airTime.Value, 0) - Tick.Delta;
				else
					airTime.Value = math.max(airTime.Value, 0) + Tick.Delta;
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