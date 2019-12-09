using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Default.Client.Visual
{
	public class CharacterBackend : RuntimeAssetBackend<CharacterPresentationBase>
	{

	}

	public abstract class CharacterPresentationBase : RuntimeAssetPresentation<CharacterPresentationBase>
	{

	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class SpawnCharacterBackendSystem : HybridEntityLinkBase<CharacterBackend>
	{
		private AssetPool<GameObject>                 m_CharacterPool;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			m_CharacterPool = new AssetPool<GameObject>(pool =>
			{
				var gameObject = new GameObject("Character Backend", typeof(CharacterBackend));
				gameObject.SetActive(false);
				gameObject.AddComponent<GameObjectEntity>();

				return gameObject;
			}, World);
		}

		public override EntityQuery GetQuery() => GetEntityQuery(typeof(CharacterDescription));

		public override void OnResult(NativeArray<Entity> backendWithoutEntity, NativeArray<Entity> entityWithoutBackend)
		{
			foreach (var backendEntity in backendWithoutEntity)
			{
				EntityManager.GetComponentObject<CharacterBackend>(backendEntity).SetDestroyFlags(0);
			}

			foreach (var entity in entityWithoutBackend)
			{
				var backend = m_CharacterPool.Dequeue().GetComponent<CharacterBackend>();
				backend.gameObject.SetActive(true);
				backend.gameObject.name = $"Character Entity={entity}";
				backend.SetTarget(EntityManager, entity);
			}
		}
	}
}