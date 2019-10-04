using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace ProKit
{
	public struct ProKitCreateCharacter
	{
		
	}
	
	public class ProKitCharacterProvider : BaseProviderBatch<ProKitCreateCharacter>
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(CharacterDescription),
				typeof(LivableDescription),
				typeof(MovableDescription),
				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld)
			};
		}

		public override void SetEntityData(Entity entity, ProKitCreateCharacter data)
		{
			
		}
	}
}