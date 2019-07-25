using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.Kits.ProKit
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class ProActionSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	[UpdateAfter(typeof(ProActionSystemGroup))]
	public class ProActionProviderFinalizeSystemGroup : ComponentSystemGroup
	{
	}
}