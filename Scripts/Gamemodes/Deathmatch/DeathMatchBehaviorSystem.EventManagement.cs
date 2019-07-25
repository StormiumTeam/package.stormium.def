using package.stormium.def.Kits.ProKit;
using Runtime.BaseSystems;
using StormiumTeam.GameBase;
using Unity.Mathematics;

namespace Stormium.Default.GameModes
{
	public partial class DeathMatchBehaviorSystem
	{
		public void ManageEvents()
		{
			var ruleGroup = World.GetExistingSystem<GameEventRuleSystemGroup>();
			ruleGroup.Process();
			
			Entities.ForEach((ref GameEvent gameEvent, ref TargetBumpEvent explosionEvent) =>
			{
				if (!EntityManager.HasComponent<Velocity>(explosionEvent.Victim))
					return;

				var velocity = EntityManager.GetComponentData<Velocity>(explosionEvent.Victim);

				velocity.Value *= math.clamp(explosionEvent.VelocityReset, 0, 1);
				velocity.Value += explosionEvent.Direction * explosionEvent.Force;
				
				EntityManager.SetComponentData(explosionEvent.Victim, velocity);

				if (EntityManager.HasComponent<ProKitMovementState>(explosionEvent.Victim))
				{
					var movementState = EntityManager.GetComponentData<ProKitMovementState>(explosionEvent.Victim);

					movementState.ForceUnground = true;
					
					EntityManager.SetComponentData<ProKitMovementState>(explosionEvent.Victim, movementState);
				}
			});
		}
	}
}