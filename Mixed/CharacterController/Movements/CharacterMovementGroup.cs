using Unity.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterInteractionGroup))]
	[UpdateAfter(typeof(CharacterMovementInitSystem))]
	[UpdateBefore(typeof(CharacterMovementEndSystem))]
	//[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class CharacterMovementGroup : ComponentSystemGroup
	{

	}
}