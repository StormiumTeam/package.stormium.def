using package.stormiumteam.shared;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit.ProGrenade;
using Stormium.Default.Kits.ProKit.ProMortar;
using Stormium.Default.Kits.ProKit.ProShotgun;
using Stormium.Default.States;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
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
		public ProjectileToTest projectileToTest = ProjectileToTest.RocketLauncher;
		public float            camIntensity     = 8f;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			// Player
			var player = dstManager.CreateEntity(typeof(GamePlayer), typeof(PlayerDescription), typeof(GamePlayerActionCommand));
			dstManager.SetComponentData(player, new GamePlayer(0, true));

			// Character
			var masterEntity  = dstManager.CreateEntity(typeof(TestProjectileSceneTag), typeof(OwnerChild));
			var livableEntity = dstManager.CreateEntity(typeof(TestProjectileSceneTag), typeof(LivableDescription));
			var movableEntity = dstManager.CreateEntity
			(
				typeof(TestProjectileSceneTag), typeof(MovableDescription), typeof(LocalCameraFreeMove), typeof(CameraModifierData),
				typeof(AimLookState), typeof(EyePosition),
				typeof(ActionContainer),
				typeof(Translation), typeof(Rotation), typeof(LocalToWorld)
			);
			
			dstManager.ReplaceOwnerData(masterEntity, player);

			dstManager.AddChildrenOwner(livableEntity, masterEntity);
			dstManager.AddChildrenOwner(movableEntity, masterEntity);

			dstManager.ReplaceOwnerData(livableEntity, masterEntity);
			dstManager.ReplaceOwnerData(movableEntity, masterEntity);

			// Camera
			var localCamera = dstManager.CreateEntity(typeof(LocalCameraState));

			dstManager.SetComponentData(localCamera, new LocalCameraState {Target         = movableEntity, Mode = CameraMode.Forced});
			dstManager.SetComponentData(movableEntity, new LocalCameraFreeMove {Intensity = camIntensity});

			// Actions
			var rocketProvider = dstManager.World.GetExistingSystem<ProRocketActionProvider>();
			var rocketAction   = rocketProvider.SpawnLocal(masterEntity, 0);

			var grenadeProvider = dstManager.World.GetExistingSystem<ProGrenadeActionProvider>();
			var grenadeAction   = grenadeProvider.SpawnLocal(masterEntity, 0);

			var mortarProvider = dstManager.World.GetExistingSystem<ProMortarActionProvider>();
			var mortarAction = mortarProvider.SpawnLocal(masterEntity, 1);

			var shotgunProvider = dstManager.World.GetExistingSystem<ProShotgunAction.Provider>();
			var shotgunAction = shotgunProvider.SpawnLocal(masterEntity, 0);
			
			dstManager.AddComponentData(rocketAction, new TestProjectileId{Value = ProjectileToTest.RocketLauncher});
			dstManager.AddComponentData(grenadeAction, new TestProjectileId{Value = ProjectileToTest.GrenadeMortar});
			dstManager.AddComponentData(mortarAction, new TestProjectileId{Value = ProjectileToTest.GrenadeMortar});
			dstManager.AddComponentData(shotgunAction, new TestProjectileId{Value = ProjectileToTest.Shotgun});

			// Singleton
			dstManager.AddComponentData(entity, new TestProjectileSceneData
			{
				Projectile    = projectileToTest,
				MovableEntity = movableEntity
			});

#if UNITY_EDITOR
			dstManager.SetName(entity, $"TestProjectile > Singleton Scene Data ({entity})");
			dstManager.SetName(player, $"TestProjectile > Player ({player})");
			dstManager.SetName(masterEntity, $"TestProjectile > Character Master ({masterEntity})");
			dstManager.SetName(livableEntity, $"TestProjectile > Character Livable ({livableEntity})");
			dstManager.SetName(movableEntity, $"TestProjectile > Character Movable ({movableEntity})");
			dstManager.SetName(localCamera, $"TestProjectile > Camera ({localCamera})");
			dstManager.SetName(rocketAction, $"TestProjectile > Rocket Action ({rocketAction})");
			dstManager.SetName(grenadeAction, $"TestProjectile > Grenade Action ({grenadeAction})");
			dstManager.SetName(mortarAction, $"TestProjectile > Mortar Action ({mortarAction})");
			dstManager.SetName(shotgunAction, $"TestProjectile > Shotgun Action ({shotgunAction})");
#endif
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