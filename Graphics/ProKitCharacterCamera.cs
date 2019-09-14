using package.stormium.def.Kits.ProKit;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Graphics
{
	[UpdateInGroup(typeof(PresentationSystemGroup)), UpdateBefore(typeof(UpdateCameraSystem))]
	public class ProKitCharacterCamera : GameBaseSystem
	{
		public struct CharacterCamera
		{
			public float RollFromInput;
			public float FocalLengthModifier;
			public float SmoothStepCooldown;

			public float3 PrevCharacterPosition;
		}

		public class GlobalSettings : ScriptableObject
		{
			public AnimationCurve FocalLengthCurve = new AnimationCurve(new[]
			{
				new Keyframe(0, 0f),
				new Keyframe(8f, 0.25f),
				new Keyframe(12f, 1.5f),
				new Keyframe(20f, 3f),
			});

			public float CurvePower                 = 3f;
			public float FocalLengthTransitionSpeed = 4f;

			public float RollPower           = 1.75f;
			public float RollTransitionSpeed = 4f;
		}

		public class GlobalSettingsComponent : MonoBehaviour
		{
			public GlobalSettings Data;
		}

		private CharacterCamera m_CameraData;

		public CharacterCamera CameraData => m_CameraData;
		public GlobalSettings  Settings;

		protected override void OnCreate()
		{
			base.OnCreate();

			Settings = ScriptableObject.CreateInstance<GlobalSettings>();
		}

		protected override void OnUpdate()
		{
			var gamePlayer = GetFirstSelfGamePlayer();
			if (gamePlayer == default)
				return;

			if (!EntityManager.HasComponent<ServerCameraState>(gamePlayer))
				return;

			var character = EntityManager.GetComponentData<ServerCameraState>(gamePlayer).Target;
			if (!EntityManager.HasComponent<ProKitMovementState>(character))
				return;

			var dt = GetTick(false).Delta;

			var controller    = EntityManager.GetComponentObject<OpenCharacterController>(character);
			var movementState = EntityManager.GetComponentData<ProKitMovementState>(character);
			var input         = EntityManager.GetComponentData<ProKitInputState>(character);
			var aim           = EntityManager.GetComponentData<AimLookState>(character).Aim;
			var velocity      = EntityManager.GetComponentData<Velocity>(character);

			var playerData = EntityManager.GetComponentData<GamePlayer>(gamePlayer);
			if (EntityManager.HasComponent<GamePlayerLocalTag>(gamePlayer))
			{
				var basicUserCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(gamePlayer);
				aim.x = basicUserCommand.Look.x;
				aim.y = basicUserCommand.Look.y;
			}

			var transform       = controller.transform;
			var position        = transform.position;
			var flatSpeed       = math.length(velocity.Value.xz);
			var horizontalInput = input.Movement.x;

			var hri = m_CameraData.RollFromInput;
			var fov = m_CameraData.FocalLengthModifier;

			var grounded = controller.isGrounded || movementState.ForceUnground;

			if (grounded && (m_CameraData.SmoothStepCooldown <= 0f || velocity.Value.y > -1f))
			{
				var distance = math.max((position.y - m_CameraData.PrevCharacterPosition.y) * 10, 10);
				position.y = Mathf.MoveTowards(Mathf.Lerp(m_CameraData.PrevCharacterPosition.y, position.y, dt * distance), position.y, dt * 2.5f);
			}
			else
			{
				m_CameraData.SmoothStepCooldown = math.select(0.1f, m_CameraData.SmoothStepCooldown - dt, grounded);
			}

			fov = math.lerp(fov, Settings.FocalLengthCurve.Evaluate(flatSpeed) * Settings.CurvePower, dt * Settings.FocalLengthTransitionSpeed);
			hri = math.lerp(hri, horizontalInput * Settings.RollPower * (grounded ? 1.0f : 0.0f), dt * Settings.RollTransitionSpeed);

			m_CameraData.RollFromInput         = hri;
			m_CameraData.FocalLengthModifier   = fov;
			m_CameraData.PrevCharacterPosition = position;

			EntityManager.SetComponentData(character, new CameraModifierData
			{
				FieldOfView = 80 + fov,
				Position    = position + new Vector3(0.0f, 1.6f, 0.0f),
				Rotation    = Quaternion.Euler(-aim.y, aim.x, -hri)
			});
		}
	}
}