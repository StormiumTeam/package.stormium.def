using Stormium.Default.States;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Default.Tests
{
	public class DummyTest : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentObject(entity, transform);
			dstManager.AddComponent(entity, typeof(LivableDescription));
			dstManager.AddComponentData(entity, new HealthState(100, 100));
		}
	}
}