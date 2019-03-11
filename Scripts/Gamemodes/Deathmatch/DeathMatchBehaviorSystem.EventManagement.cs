using package.stormium.def.Kits.ProKit;
using Stormium.Default.States;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using UnityEngine;

namespace Stormium.Default.GameModes
{
	public partial class DeathMatchBehaviorSystem
	{
		public void ManageEvents()
		{
			ForEach((ref GameEvent gameEvent, ref TargetDamageEvent damageEvent) =>
			{
				Debug.Log($"TargetDamageEvent({damageEvent.Victim})");
				
				if (!EntityManager.HasComponent<HealthState>(damageEvent.Victim))
					return;
				if (EntityManager.HasComponent<OwnerState<LivableDescription>>(damageEvent.Shooter))
				{
					var livable = EntityManager.GetComponentData<OwnerState<LivableDescription>>(damageEvent.Shooter);
					if (livable.Target == damageEvent.Victim) // no self damage for now
						damageEvent.DmgValue = (int)(damageEvent.DmgValue / 2.5f);
				}

				var healthState = EntityManager.GetComponentData<HealthState>(damageEvent.Victim);
				healthState.Health -= damageEvent.DmgValue;
				
				EntityManager.SetComponentData(damageEvent.Victim, healthState);
			});
			
			ForEach((ref GameEvent gameEvent, ref TargetBumpEvent explosionEvent) =>
			{
				if (!EntityManager.HasComponent<Velocity>(explosionEvent.Victim))
					return;

				var velocity = EntityManager.GetComponentData<Velocity>(explosionEvent.Victim);
				velocity.Value += explosionEvent.Direction * explosionEvent.Force;
				
				EntityManager.SetComponentData(explosionEvent.Victim, velocity);

				if (EntityManager.HasComponent<ProKitMovementState>(explosionEvent.Victim))
				{
					var movementState = EntityManager.GetComponentData<ProKitMovementState>(explosionEvent.Victim);

					movementState.ForceUnground = 1;
					
					EntityManager.SetComponentData<ProKitMovementState>(explosionEvent.Victim, movementState);
				}
			});
		}
	}
}