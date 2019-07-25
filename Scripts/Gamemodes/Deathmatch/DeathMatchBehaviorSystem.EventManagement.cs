using package.stormium.def.Kits.ProKit;
using Runtime.BaseSystems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Mathematics;

namespace Stormium.Default.GameModes
{
	public partial class DeathMatchBehaviorSystem
	{
		public void ManageEvents()
		{
			var ruleGroup = World.GetExistingSystem<GameEventRuleSystemGroup>();
			ruleGroup.Process();

			Entities.ForEach((ref GameEvent gameEvent, ref TargetImpulseEvent explosionEvent) =>
			{
				if (!EntityManager.HasComponent<Velocity>(explosionEvent.Destination))
					return;

				var velocity = EntityManager.GetComponentData<Velocity>(explosionEvent.Destination);

				velocity.Value *= math.clamp(explosionEvent.Momentum, 0, 1);
				velocity.Value += explosionEvent.Force;

				EntityManager.SetComponentData(explosionEvent.Destination, velocity);

				if (EntityManager.HasComponent<ProKitMovementState>(explosionEvent.Destination))
				{
					var movementState = EntityManager.GetComponentData<ProKitMovementState>(explosionEvent.Destination);

					movementState.ForceUnground = true;

					EntityManager.SetComponentData<ProKitMovementState>(explosionEvent.Destination, movementState);
				}
			});
		}
	}
}