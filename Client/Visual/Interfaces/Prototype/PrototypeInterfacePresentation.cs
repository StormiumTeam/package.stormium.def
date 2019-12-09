using System;
using DefaultNamespace;
using Unity.NetCode;
using Stormium.Default.Mixed;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Modules;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Default.Client.Visual.Interfaces.Prototype
{
	public class PrototypeInterfacePresentation : RuntimeAssetPresentation<PrototypeInterfacePresentation>
	{
		public class EmptyBackend : RuntimeAssetBackend<PrototypeInterfacePresentation>
		{
		}

		public GaugeDefinition HealthGaugeDefinition;
		public GaugeDefinition ArmorGaugeDefinition;
		public ReticleDefinition ReticleDefinition;
		public AmmoDefinition AmmoDefinition;
		public Animator HitIndicatorAnimator;
	}
	
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UpdatePrototypeInterfaceDidHitVariable : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((ref TargetDamageEvent damageEv) =>
			{
				var copy = damageEv;
				if (EntityManager.HasComponent<Relative<CharacterDescription>>(copy.Origin))
					copy.Origin = EntityManager.GetComponentData<Relative<CharacterDescription>>(copy.Origin).Target;
				
				World.GetExistingSystem<PrototypeInterfaceRenderSystem>()
				     .DamageEvents.Add(copy);
			});
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(PrototypeInterfaceSpawnSystem))]
	public class PrototypeInterfaceRenderSystem : RenderSystem<PrototypeInterfacePresentation>
	{
		public int HealthValue, HealthMax;
		public int ArmorValue,  ArmorMax;

		public bool IsReloading;
		public int  ReloadingProgress, ReloadingTime;

		public int  Ammo, AmmoMax;
		public NativeList<TargetDamageEvent> DamageEvents;
		public Entity Spectated;
		
		private static readonly int s_Hit = Animator.StringToHash("Hit");

		private bool TryGetComponentData<T>(Entity entity, out T data, T def = default)
			where T : struct, IComponentData
		{
			if (EntityManager.HasComponent<T>(entity))
			{
				data = EntityManager.GetComponentData<T>(entity);
				return true;
			}

			data = def;
			return false;
		}

		public override void PrepareValues()
		{
			var gamePlayer = GetFirstSelfGamePlayer();
			if (gamePlayer == default)
				return;

			if (!TryGetCurrentCameraState(gamePlayer, out var camState))
				return;

			Spectated = camState.Target;
			if (TryGetComponentData<LivableHealth>(camState.Target, out var livableHealth))
			{
				HealthValue = livableHealth.Value;
				HealthMax   = livableHealth.Max;
			}

			if (TryGetComponentData<CurrentWeapon>(camState.Target, out var currentWeapon))
			{
				if (TryGetComponentData<ReloadingState>(currentWeapon.Target, out var reloadingState))
				{
					IsReloading       = reloadingState.Active;
					ReloadingProgress = reloadingState.Progress.Value;
					ReloadingTime     = reloadingState.TimeToReload;
				}

				if (TryGetComponentData<ActionAmmo>(currentWeapon.Target, out var ammo))
				{
					Ammo    = ammo.Value;
					AmmoMax = ammo.Max;
				}
			}
		}

		public override void Render(PrototypeInterfacePresentation definition)
		{
			definition.HealthGaugeDefinition.Set(HealthValue, HealthMax);
			definition.ReticleDefinition.SetActive(IsReloading);
			definition.ReticleDefinition.SetProgression(ReloadingProgress, ReloadingTime);
			definition.AmmoDefinition.Set(Ammo, AmmoMax);
			
			var baseline = default(TargetDamageEvent);
			if (((NativeArray<TargetDamageEvent>) DamageEvents).Contains(ref baseline, ref baseline.Origin, Spectated))
				definition.HitIndicatorAnimator.SetTrigger(s_Hit);
		}

		public override void ClearValues()
		{
			IsReloading = false;
			DamageEvents.Clear();
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			
			DamageEvents = new NativeList<TargetDamageEvent>(Allocator.Persistent);
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class PrototypeInterfaceSpawnSystem : SpawnSystem<PrototypeInterfacePresentation.EmptyBackend, PrototypeInterfacePresentation>
	{
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(DeathMatchGameMode));
		}

		protected override string AddressableAsset => "prototype_interface";
	}

	public abstract class SpawnSystem<TBackend, TPresentation> : GameBaseSystem
		where TBackend : RuntimeAssetBackend<TPresentation>
		where TPresentation : RuntimeAssetPresentation<TPresentation>
	{
		private GetAllBackendModule<TBackend> m_Module;

		private AssetPool<GameObject> m_BackendPool;
		public  AssetPool<GameObject> BackendPool => m_BackendPool;

		private AsyncAssetPool<GameObject> m_PresentationPool;
		public  AsyncAssetPool<GameObject> PresentationPool => m_PresentationPool;

		protected abstract EntityQuery GetQuery();

		protected virtual void CreatePoolBackend(out AssetPool<GameObject> pool)
		{
			pool = new AssetPool<GameObject>(p =>
			{
				var go = new GameObject($"pooled={GetType().Name}");
				go.SetActive(false);

				go.AddComponent<TBackend>();
				go.AddComponent<GameObjectEntity>();

				return go;
			}, World);
		}

		protected abstract string AddressableAsset { get; }

		protected virtual void CreatePoolPresentation(out AsyncAssetPool<GameObject> pool)
		{
			if (AddressableAsset == null)
				throw new NullReferenceException($"{nameof(AddressableAsset)} is null, did you mean to replace 'CreatePoolPresentation' ?");

			pool = new AsyncAssetPool<GameObject>(AddressableAsset);
		}


		private EntityQuery m_Query;

		protected TBackend LastBackend { get; set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_Module);
			CreatePoolBackend(out m_BackendPool);
			CreatePoolPresentation(out m_PresentationPool);

			m_Query = GetQuery();
		}

		protected override void OnUpdate()
		{
			if (m_Query.IsEmptyIgnoreFilter)
				return;

			m_Module.TargetEntities = m_Query.ToEntityArray(Allocator.TempJob);
			m_Module.Update(default).Complete();
			m_Module.TargetEntities.Dispose();

			foreach (var backendWithoutModel in m_Module.BackendWithoutModel)
			{
				ReturnBackend(EntityManager.GetComponentObject<TBackend>(backendWithoutModel));
			}

			foreach (var entityWithoutBackend in m_Module.MissingTargets)
			{
				SpawnBackend(entityWithoutBackend);
			}
		}

		protected virtual void ReturnBackend(TBackend backend)
		{
			backend.Return(true, true);
		}

		protected virtual void SpawnBackend(Entity target)
		{
			var gameObject = m_BackendPool.Dequeue();
			gameObject.SetActive(true);

			gameObject.name = $"{target} '{GetType().Name}' Backend";

			var backend = gameObject.GetComponent<TBackend>();
			backend.SetTarget(EntityManager, target);
			backend.SetPresentationFromPool(m_PresentationPool);

			LastBackend = backend;
		}
	}
}