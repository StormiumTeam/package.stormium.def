using Stormium.Core;
using Unity.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace CharacterController
{
	[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
	[UpdateAfter(typeof(BasicUserCommandUpdatePredicted))]
	public class CharacterInteractionGroup : ComponentSystemGroup
	{
	}
}