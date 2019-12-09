using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;

namespace DefaultNamespace.Components.InGame
{
	public class KillZoneAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponent(entity, typeof(KillZone));
			dstManager.AddComponent(entity, typeof(IgnorePhysicsWorld));
		}
	}
	
	public struct KillZone : IComponentData
	{
		// what should we add here?
	}
}