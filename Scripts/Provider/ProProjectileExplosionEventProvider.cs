using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Stormium.Default.Kits.ProKit
{
	public class DamageEventProvider : BaseProviderBatch<TargetDamageEvent>
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(GameEvent),
				typeof(TargetDamageEvent),
			};
		}

		public override void SetEntityData(Entity entity, TargetDamageEvent data)
		{
			EntityManager.SetComponentData(entity, data);
		}
	}
}