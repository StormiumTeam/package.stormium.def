using package.stormiumteam.shared;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit.ProGrenade;
using Stormium.Default.Kits.ProKit.ProMortar;
using Stormium.Default.Kits.ProKit.ProShotgun;
using Stormium.Default.States;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Stormium.Default.Tests.projectiles
{
	public enum ProjectileToTest
	{
		RocketLauncher,
		RailGun,
		GrenadeMortar,
		Shotgun
	}
	
	public struct TestProjectileId : IComponentData
	{
		public ProjectileToTest Value;
	}
	
	public struct TestProjectileSceneTag : IComponentData
	{}

	public struct TestProjectileSceneData : IComponentData
	{
		public ProjectileToTest Projectile;

		public Entity MovableEntity;
	}

	public class TestProjectileScene : MonoBehaviour, IConvertGameObjectToEntity
	{
		public ProjectileToTest projectileToTest = ProjectileToTest.Shotgun;
		public float            camIntensity     = 8f;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			// Player
			var player = dstManager.CreateEntity(typeof(GamePlayer), typeof(PlayerDescription), typeof(GamePlayerActionCommand));
			dstManager.SetComponentData(player, new GamePlayer(0, true));

			// Character
			var masterEntity = dstManager.CreateEntity(typeof(TestProjectileSceneTag),
				// --- LIVABLE ---
				typeof(LivableDescription),
				typeof(ActionContainer),
				// --- MOVABLE ---
				typeof(MovableDescription), typeof(LocalCameraFreeMove), typeof(CameraModifierData),
				typeof(AimLookState), typeof(EyePosition),
				typeof(Translation), typeof(Rotation), typeof(LocalToWorld),
				// other...
				typeof(OwnerChild));

			dstManager.ReplaceOwnerData(masterEntity, player);

			// Camera
			var localCamera = dstManager.CreateEntity(typeof(LocalCameraState));
			dstManager.SetComponentData(localCamera, new LocalCameraState
			{
				Data = new CameraState
				{
					Target = masterEntity, Mode = CameraMode.Forced
				}
			});
			dstManager.SetComponentData(masterEntity, new LocalCameraFreeMove {Intensity = camIntensity});

			void CreateAction<TProvider, TActionCreate>(TActionCreate create, ProjectileToTest projectileToTest)
				where TProvider : BaseProviderBatch<TActionCreate>
				where TActionCreate : struct
			{
				using (var entities = new NativeList<Entity>(1, Allocator.TempJob))
				{
					var provider = dstManager.World.GetExistingSystem<TProvider>();
					provider.SpawnLocalEntityWithArguments(create, entities);

					dstManager.AddComponentData(entities[0], new TestProjectileId {Value = projectileToTest});
#if UNITY_EDITOR
					dstManager.SetName(entities[0], $"TestProjectile > {projectileToTest} Action ({entities[0]})");
#endif
				}
			}

			// Actions
			CreateAction<ProShotgunAction.Provider, ProShotgunAction.Create>(new ProShotgunAction.Create
			{
				Slot  = 0,
				Owner = masterEntity,
			}, ProjectileToTest.Shotgun);

			// Singleton
			dstManager.AddComponentData(entity, new TestProjectileSceneData
			{
				Projectile    = projectileToTest,
				MovableEntity = masterEntity
			});


			dstManager.SetName(entity, $"TestProjectile > Singleton Scene Data ({entity})");
			dstManager.SetName(player, $"TestProjectile > Player ({player})");
			dstManager.SetName(masterEntity, $"TestProjectile > Character Master ({masterEntity})");
			dstManager.SetName(localCamera, $"TestProjectile > Camera ({localCamera})");
		}
	}

	public class TestProjectileSystem : ComponentSystem, INativeEventOnGUI
	{
		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			World.GetOrCreateSystem<AppEventSystem>().SubscribeToAll(this);
		}

		protected override void OnUpdate()
		{
			var sceneData = GetSingleton<TestProjectileSceneData>();

			if (Keyboard.current.digit1Key.isPressed) sceneData.Projectile = ProjectileToTest.RocketLauncher;
			if (Keyboard.current.digit2Key.isPressed) sceneData.Projectile = ProjectileToTest.RailGun;
			if (Keyboard.current.digit3Key.isPressed) sceneData.Projectile = ProjectileToTest.GrenadeMortar;
			if (Keyboard.current.digit4Key.isPressed) sceneData.Projectile = ProjectileToTest.Shotgun;

			SetSingleton(sceneData);

			Entities.WithAll<TestProjectileSceneTag>().ForEach((Entity entity, ref LocalToWorld localToWorld, ref AimLookState aimLookState) =>
			{
				var eulerAngles = Quaternion.LookRotation(localToWorld.Forward).eulerAngles;

				aimLookState.Aim = new float2(eulerAngles.y, -eulerAngles.x);
			});

			Entities.WithAll<Disabled>().With(EntityQueryOptions.IncludeDisabled).ForEach((Entity entity, ref TestProjectileId projectileId) =>
			{
				if (projectileId.Value == sceneData.Projectile)
					PostUpdateCommands.RemoveComponent<Disabled>(entity);
			});

			Entities.ForEach((Entity entity, ref TestProjectileId projectileId) =>
			{
				if (projectileId.Value != sceneData.Projectile)
					PostUpdateCommands.AddComponent(entity, new Disabled());
			});
		}

		private bool Has<T>(Entity e)
			where T : struct, IComponentData
		{
			return EntityManager.HasComponent<T>(e);
		}

		public void NativeOnGUI()
		{
			GUILayout.Space(10);

			GUI.contentColor = Color.white;
			GUI.color = Color.white;
			
			var style = new GUIStyle(GUI.skin.label) {active = {textColor = Color.white}};

			GUILayout.FlexibleSpace();
			
			using (new GUILayout.VerticalScope())
			{
				GUILayout.Label("Current Weapon [" + GetSingleton<TestProjectileSceneData>().Projectile + "]", style);
			}
		}
	}
}