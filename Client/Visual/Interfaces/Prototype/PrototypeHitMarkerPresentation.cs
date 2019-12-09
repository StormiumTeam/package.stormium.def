using CharacterController;
using DefaultNamespace;
using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Client.Visual.Interfaces.Prototype
{
	public class PrototypeHitMarkerPresentation : RuntimeAssetPresentation<PrototypeHitMarkerPresentation>
	{
		public class InnerBackend : RuntimeAssetBackend<PrototypeHitMarkerPresentation>
		{
			public float  TimeBeforePooling { get; set; }
			public float3 Position          { get; set; }
			public int    Damage            { get; set; }
			public Camera Camera            { get; set; }
			public Entity Destination       { get; set; }
		}

		public TextMeshProUGUI Label;

		private Camera cameraTarget;
		private float3 position;

		public override void OnBackendSet()
		{
			var b = Backend as InnerBackend;
			Label.text   = b.Damage.ToString();
			position     = b.Position;
			cameraTarget = b.Camera;

			GetComponent<Canvas>().enabled = true;
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		[UpdateAfter(typeof(UpdateCameraSystem))]
		public class System : GameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((PrototypeHitMarkerPresentation presentation) =>
				{
					var target = presentation.cameraTarget.WorldToScreenPoint(presentation.position);
					if (target.z >= 0)
					{
						presentation.Label.alpha              = 1;
						presentation.Label.transform.position = target;
					}
					else
					{
						presentation.Label.alpha = 0;
					}
				});
			}
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class SpawnSystem : SpawnSystem<PrototypeHitMarkerPresentation.InnerBackend, PrototypeHitMarkerPresentation>
	{
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(TargetDamageEvent));
		}

		protected override string AddressableAsset => "prototype_hitmarker";

		private static int m_PoolVolley = 0;

		protected override void OnUpdate()
		{
			m_PoolVolley = 0;
			Entities.ForEach((PrototypeHitMarkerPresentation.InnerBackend backend) =>
			{
				backend.TimeBeforePooling -= GetTick(false).Delta;
				if (backend.TimeBeforePooling > 0.0f)
					return;

				if (m_PoolVolley < 5 && backend.Presentation != null)
				{
					m_PoolVolley++;
					backend.Return(false, false);
					Object.Destroy(backend.gameObject.GetComponent<GameObjectEntity>());
					backend.Presentation.GetComponent<Canvas>().enabled = false;
				}
			});

			base.OnUpdate();
		}

		protected override void CreatePoolBackend(out AssetPool<GameObject> pool)
		{
			pool = new AssetPool<GameObject>(p =>
			{
				var go = new GameObject($"pooled={GetType().Name}");

				go.AddComponent<PrototypeHitMarkerPresentation.InnerBackend>();
				go.AddComponent<GameObjectEntity>();

				return go;
			}, World);
		}

		protected override void ReturnBackend(PrototypeHitMarkerPresentation.InnerBackend backend)
		{
		}

		private GameObject FindBackend(Entity dmgDestination)
		{
			using (var entities = Entities.WithAll<PrototypeHitMarkerPresentation.InnerBackend>().ToEntityQuery()
			                              .ToEntityArray(Allocator.TempJob))
			{
				foreach (var ent in entities)
				{
					var backend = EntityManager.GetComponentObject<PrototypeHitMarkerPresentation.InnerBackend>(ent);
					if (backend.Destination != dmgDestination || backend.TimeBeforePooling < 0.75f)
						continue;

					return backend.gameObject;
				}
			}

			return null;
		}

		protected override void SpawnBackend(Entity target)
		{
			var gp = GetFirstSelfGamePlayer();
			if (gp == default)
				return;

			if (!TryGetCurrentCameraState(gp, out var camState) || camState.Target == default)
				return;

			var dmg = EntityManager.GetComponentData<TargetDamageEvent>(target);
			if (dmg.Origin != camState.Target || dmg.Destination == camState.Target)
				return;

			var cumulation = false;
			var gameObject = FindBackend(dmg.Destination);
			if (gameObject != null)
				cumulation = true;
			else
				gameObject = BackendPool.Dequeue();

			gameObject.name = $"{target} '{GetType().Name}' Backend";

			var translation = EntityManager.GetComponentData<Translation>(dmg.Destination);
			if (EntityManager.HasComponent(target, typeof(TranslationSnapshot)))
			{
				var b = EntityManager.GetBuffer<TranslationSnapshot>(target);
				b[b.Length - 1].SynchronizeTo(ref translation, default);
				Debug.Log($"{b[0].Value}, {b[b.Length - 1].Value}");
			}

			var backend = gameObject.GetComponent<PrototypeHitMarkerPresentation.InnerBackend>();
			backend.Destination       = dmg.Destination;
			backend.TimeBeforePooling = 1f;
			backend.Position          = translation.Value;
			backend.Damage            = cumulation ? backend.Damage + math.abs(dmg.Damage) : math.abs(dmg.Damage);
			backend.Camera            = World.GetExistingSystem<ClientCreateCameraSystem>().Camera;

			backend.SetTarget(EntityManager, target);
			if (!backend.HasIncomingPresentation)
				backend.SetPresentationFromPool(PresentationPool);
			else
			{
				if (backend.gameObject.GetComponent<GameObjectEntity>() == null)
					backend.gameObject.AddComponent<GameObjectEntity>();

				backend.Presentation.OnBackendSet();
			}

			LastBackend = backend;
		}
	}
}