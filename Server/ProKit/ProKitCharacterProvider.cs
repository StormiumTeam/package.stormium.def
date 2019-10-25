using CharacterController;
using package.stormium.def;
using Stormium.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
				typeof(CharacterDescription),
				typeof(LivableDescription),
				typeof(HealthContainer),
				typeof(LivableHealth),
				typeof(AimLookState),
				typeof(HealthModifyingHistory),
				typeof(MovableDescription),
				typeof(PhysicsCharacter),
				typeof(Stamina),
				typeof(SrtGroundMovementComponent),
				typeof(SrtAerialMovementComponent),
				typeof(AirTime),
				typeof(SrtJumpMovementComponent),
				typeof(SrtDodgeMovementComponent),
				typeof(SrtWallBounceMovementComponent),
				typeof(CharacterInput),
				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),
				typeof(Velocity),
				typeof(PhysicsCollider)
			};
		}

		public override void SetEntityData(Entity entity, ProKitCreateCharacter data)
		{
			EntityManager.SetComponentData(entity, new PhysicsCharacter
			{
				MaxStepHeight = 0.5f
			});
			EntityManager.SetComponentData(entity, new Stamina {Value = 0f, Max = 1f, GainPerSecond = 0.18f});
			EntityManager.SetComponentData(entity, new SrtGroundMovementComponent
			{
				Settings  = SrtGroundSettings.NewBase(),
				CanCrouch = true,
				CanSprint = true
			});
			EntityManager.SetComponentData(entity, new SrtAerialMovementComponent
			{
				Settings             = SrtAerialSettings.NewBase(),
				AirControl           = 1.0f,
				AirControlResistance = 20f,
				Drag = 0.0675f
			});
			EntityManager.SetComponentData(entity, new SrtJumpMovementComponent
			{
				IsJumpingInChain = false,
				JumpQueued = default,
				StaminaUsageOnStandardJump = StaminaUsage.FromPercentage(0.05f),
				StaminaUsageOnChainingJump = StaminaUsage.FromPercentage(0.13f)
			});
			EntityManager.SetComponentData(entity, new SrtDodgeMovementComponent
			{
				EnableGroundDodge = true,
				StaminaUsage = StaminaUsage.FromPercentage(0.3f)
			});
			EntityManager.SetComponentData(entity, new SrtWallBounceMovementComponent
			{
				EnableWallDodge = true,
				EnableWallJump  = true,
				StaminaUsageOnWallJump = StaminaUsage.FromPercentage(0.3f),
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