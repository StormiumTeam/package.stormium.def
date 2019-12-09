using CharacterController;
using package.stormium.def;
using Stormium.Core.Data;
using Stormium.Default;
using Stormium.Default.Mixed;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace ProKit
{


	public struct ProKitCreateCharacter
	{

	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
	public class ProKitCharacterProvider : BaseProviderBatch<ProKitCreateCharacter>
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(CharacterComponent),
				typeof(CharacterPass),
				typeof(LivableHealth),
				typeof(HealthModifyingHistory),
				typeof(CurrentWeapon),
				
				// --- Descriptions
				typeof(CharacterDescription),
				typeof(WeaponHolderDescription),
				typeof(ActionHolderDescription),
				typeof(LivableDescription),
				typeof(MovableDescription),

				// -- Transforms
				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),
				typeof(TransformHistory),

				// -- States
				typeof(Stamina),
				typeof(AirTime),
				typeof(CharacterInput),
				typeof(AimLookState),

				// --- Movements
				typeof(StandardJumpMovement),
				typeof(StandardDodgeMovement),
				typeof(StandardWallBounce),
				typeof(StandardGroundMovement),
				typeof(StandardAerialMovement),

				// - Physics
				typeof(PhysicsCollider),
				typeof(PhysicsCharacter),
				typeof(Velocity),

				// --- Containers
				typeof(ActionContainer),
				typeof(HitShapeContainer),
				
				typeof(GhostPredictedComponent)
			};
		}

		public override void SetEntityData(Entity entity, ProKitCreateCharacter data)
		{
			EntityManager.SetComponentData(entity, new PhysicsCharacter
			{
				MaxStepHeight = 0.5f
			});
			EntityManager.SetComponentData(entity, new Stamina {Value = 0f, Max = 1f, GainPerSecond = 0.18f});
			EntityManager.SetComponentData(entity, new StandardGroundMovement
			{
				Settings  = SrtGroundSettings.NewBase(),
				CanCrouch = true,
				CanSprint = true
			});
			EntityManager.SetComponentData(entity, new StandardAerialMovement
			{
				Settings             = SrtAerialSettings.NewBase(),
				AirControl           = 1.0f,
				AirControlResistance = 20f,
				Drag                 = 0.08f
			});
			EntityManager.SetComponentData(entity, new StandardJumpMovement
			{
				IsJumpingInChain           = false,
				JumpQueued                 = default,
				StaminaUsageOnStandardJump = StaminaUsage.FromPercentage(0.05f),
				StaminaUsageOnChainingJump = StaminaUsage.FromPercentage(0.13f)
			});
			EntityManager.SetComponentData(entity, new StandardDodgeMovement
			{
				EnableGroundDodge = true,
				StaminaUsage      = StaminaUsage.FromPercentage(0.3f)
			});
			EntityManager.SetComponentData(entity, new StandardWallBounce
			{
				EnableWallDodge         = true,
				EnableWallJump          = true,
				StaminaUsageOnWallJump  = StaminaUsage.FromPercentage(0.2f),
				StaminaUsageOnWallDodge = StaminaUsage.FromAbsolute(0.3f)
			});
			EntityManager.SetComponentData(entity, new PhysicsCollider
			{
				Value = CapsuleCollider.Create(new CapsuleGeometry
				{
					Radius  = 0.5f,
					Vertex0 = 0,
					Vertex1 = new float3(0, 1, 0)
				}, new CollisionFilter
				{
					BelongsTo    = 0b00000000000000010000000000000000,
					CollidesWith = 0b11111111111111101111111111111111
				})
			});
		}
	}
}