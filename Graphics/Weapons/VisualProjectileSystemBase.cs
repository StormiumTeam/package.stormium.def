using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Graphics.Weapons
{
	public abstract class VisualProjectileSystemBase<TProjectile, TPresentation, TBackend> : ComponentSystem
		where TProjectile : struct, IComponentData
		where TPresentation : CustomAsyncAssetPresentation<TPresentation>
		where TBackend : CustomAsyncAsset<TPresentation>
	{
		public struct ComponentState : IComponentData
		{
			public int PoolIdx;
		}

		protected AsyncAssetPool<GameObject> PresentationPool;
		protected AssetPool<GameObject>      BackendPool;

		protected EntityQuery QueryWithoutState;
		protected EntityQuery QueryBackend;

		protected abstract string PresentationAssetId { get; }

		protected virtual void SetPools()
		{
			PresentationPool = new AsyncAssetPool<GameObject>(PresentationAssetId);
			BackendPool      = new AssetPool<GameObject>(SetBackendGameObjectCreation);
		}

		protected virtual GameObject SetBackendGameObjectCreation()
		{
			var go = new GameObject("Disabled " + GetType(), typeof(GameObjectEntity), typeof(TBackend));
			go.SetActive(false);

			go.GetComponent<TBackend>().SetRootPool(BackendPool);

			return go;
		}

		protected virtual void SetQueries()
		{
			QueryWithoutState = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(TProjectile)},
				None = new ComponentType[] {typeof(ComponentState)}
			});
			QueryBackend = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(TBackend)}
			});
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			SetPools();
			SetQueries();
		}

		private void CreateVisualProjectiles()
		{
			var entities = QueryWithoutState.ToEntityArray(Allocator.TempJob);

			EntityManager.AddComponent(QueryWithoutState, typeof(ComponentState));
			foreach (var entity in entities)
			{
				var backendGameObject = BackendPool.Dequeue();
				backendGameObject.name = $"{GetType()}({entity})";
				backendGameObject.SetActive(true);

				var backend = backendGameObject.GetComponent<TBackend>();
				backend.OnReset();
				backend.SetFromPool(PresentationPool, EntityManager, entity);

				EntityManager.SetComponentData(entity, new ComponentState {PoolIdx = BackendPool.IndexOf(backendGameObject)});
			}

			entities.Dispose();
		}

		protected bool CheckAndDisableForNextFrame(TBackend backend)
		{
			if (backend.DstEntityManager.Exists(backend.DstEntity))
				return false;

			backend.DisableNextUpdate                 = true;
			backend.ReturnToPoolOnDisable             = true;
			backend.ReturnPresentationToPoolNextFrame = true;

			return true;
		}

		protected override void OnUpdate()
		{
			CreateVisualProjectiles();
		}
	}
}