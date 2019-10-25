using Stormium.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterInteractionGroup))]
	public class CharacterResetAirControlSystem : JobComponentSystem
	{
		private struct JobGatherEvent : IJobForEach_C<TargetImpulseEvent>
		{
			public ComponentDataFromEntity<SrtAerialMovementComponent> AerialComponentFromEntity;

			public void Execute([ReadOnly] ref TargetImpulseEvent impulseEvent)
			{
				if (impulseEvent.Destination == default)
					return;

				if (AerialComponentFromEntity.Exists(impulseEvent.Destination))
				{
					var aerialComponent = AerialComponentFromEntity[impulseEvent.Destination];
					aerialComponent.AirControl *= aerialComponent.AirControlResistance - aerialComponent.AirControlResistance * math.length(impulseEvent.Force);

					AerialComponentFromEntity[impulseEvent.Destination] = aerialComponent;
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new JobGatherEvent
			{
				AerialComponentFromEntity = GetComponentDataFromEntity<SrtAerialMovementComponent>()
			}.ScheduleSingle(this, inputDeps);
		}
	}
}