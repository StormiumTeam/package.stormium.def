using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
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
			dstManager.AddComponent(entity, typeof(LivableHealth));
			dstManager.AddComponent(entity, typeof(HealthContainer));
			dstManager.AddComponentData(entity, new Relative<LivableDescription> {Target = entity});

			using (var healthEntities = new NativeList<Entity>(Allocator.TempJob))
			{
				var data = new RegenerativeHealthData.CreateInstance
				{
					value = 100,
					max   = 100,
					cooldown = 3000,
					rate = 50f,
					owner = entity
				};
				dstManager.World.GetOrCreateSystem<RegenerativeHealthData.InstanceProvider>().SpawnLocalEntityWithArguments(data, healthEntities);
			}
		}
	}
}