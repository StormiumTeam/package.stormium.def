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
	[UpdateAfter(typeof(CharacterMovementGroup))]
	[UpdateBefore(typeof(CharacterMovementEndSystem))]
	public class CharacterUpdateStaminaSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobForEach_C<Stamina>
		{
			[ReadOnly]
			public UTick Tick;

			public void Execute(ref Stamina stamina)
			{
				var maxStamina = math.max(stamina.Value, stamina.Max);
				stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * Tick.Delta, 0, maxStamina);
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