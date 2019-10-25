using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace CharacterController
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(WorldType.ServerWorld)]
	public class CharacterInteractionGroup : ComponentSystemGroup
	{
		
	}
}