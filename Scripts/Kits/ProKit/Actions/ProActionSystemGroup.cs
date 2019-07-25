using Scripts.Actions.ProKitWeapons;
using Scripts.Actions.ProRailgun;
using Stormium.Default.Actions.ProMinigun;
using Stormium.Default.Kits.ProKit.ProGrenade;
using Stormium.Default.Kits.ProKit.ProMortar;
using Stormium.Default.Kits.ProKit.ProShotgun;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.Kits.ProKit
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class ProActionSystemGroup : ComponentSystemGroup
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			AddSystemToUpdateList(World.GetOrCreateSystem<ProRocketActionUpdateSystem>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProRocketDetonateActionUpdateSystem>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProRailgunActionSystem>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProMinigunActionSystem>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProGrenadeActionProcess>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProMortarActionProcess>());
			AddSystemToUpdateList(World.GetOrCreateSystem<ProShotgunAction.Process>());
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			World.GetExistingSystem<ProGrenadeProjectile.Provider>().FlushDelayedEntities();
			World.GetExistingSystem<ProMortarProjectileProvider>().FlushDelayedEntities();
			World.GetExistingSystem<ProShotgunProjectile.Provider>().FlushDelayedEntities();
		}
	}
}