using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.Actions.ProMinigun
{
	public struct ProMinigunProjectile : IComponentData
	{
		
	}
	
	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProMinigunProjectileSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			
		}
	}
}