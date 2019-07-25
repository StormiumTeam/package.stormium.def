using package.stormium.def;
using Unity.Mathematics;

namespace Stormium.Default
{
	public struct SrtMovementAccessor
	{
		/// <summary>
		/// True if we can made movement to the character.
		/// False if we can't (eg: we are dead, or we have some effects that don't make us move)
		/// </summary>
		public bool CanUseMovements;
	}
	
	public struct SrtGroundMovementData
	{
		public SrtGroundSettings Settings;
		public bool CanCrouch;
		public bool CanSprint;
		
		public bool IsSprinting;
		public bool IsCrouched;
	}

	public struct SrtAerialMovementData
	{
		public SrtAerialSettings Settings;
	}

	public struct SrtSlideMovementData
	{
		//public SrtSlideSettings Settings;
		
		/// <summary>
		/// Automatically managed. If it's enabled, you can gain some speed by doing sharp angles.
		/// It's automatically disabled if we don't have enough stamina.
		/// </summary>
		public bool CanGainSpeed;

		/// <summary>
		/// Should we loose stamina if we are sliding?
		/// A recommended value would be 5% per second
		/// </summary>
		public float LooseStaminaOnSliding;
		/// <summary>
		/// Should we loose stamina if we have too much speed?
		/// A recommended value would be 1. (the movement system should automatically do stamina -= excessSpeed * StaminaFactorOnExcess)
		/// </summary>
		public float StaminaFactorOnExcess;
		
		public float3 SlideNormal;
		
		public bool IsSliding;
		public bool IsWallSliding;
	}

	public struct SrtDodgeMovementData
	{
		/// <summary>
		/// Automatically managed. If it's enabled, we can make some dodges.
		/// It's automatically disabled if we don't have enough stamina
		/// </summary>
		public bool HasEnoughStamina;
		public float StaminaUsage;
		
		public bool EnableGroundDodge, EnableAerialDodge;
	}

	public struct SrtWallBounceMovementData
	{
		public bool HasEnoughStamina;
		public float StaminaUsage;
		
		public bool EnableWallDodge, EnableWallJump;
		public bool DistinctBounceType;

		/// <summary>
		/// Only work if <see cref="DistinctBounceType"/> is enabled
		/// </summary>
		public float MinVerticalVelocityForWallJump;
	}
}