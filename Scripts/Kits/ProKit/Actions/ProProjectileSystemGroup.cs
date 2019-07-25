using Scripts.Actions.ProRailgun;
using Stormium.Default.Actions.ProMinigun;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.Kits.ProKit
{
	[UpdateInGroup(typeof(ProjectileSystemGroup))]
	public class ProProjectileSystemGroup : ComponentSystemGroup
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			AddSystemToUpdateList(World.GetOrCreateSystem<ProMinigunProjectileSystem>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProRailgunProjectileSystem>());
		}

		public override void SortSystemUpdateList()
		{
			base.SortSystemUpdateList();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
		}
	}
}