using package.stormium.def;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;

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

	public struct SrtGroundMovementComponent : IComponentData
	{
		public SrtGroundSettings Settings;
		public bool              CanCrouch;
		public bool              CanSprint;

		public bool IsSprinting;
		public bool IsCrouched;

		public void Move(ref float3 velocity)
		{

		}
	}

	public struct SrtJumpMovementComponent : IComponentData
	{
		public bool  IsJumpingInChain;
		public UTick JumpQueued;

		public StaminaUsage StaminaUsageOnChainingJump;
		public StaminaUsage StaminaUsageOnStandardJump;
	}

	public struct SrtAerialMovementComponent : IComponentData
	{
		public SrtAerialSettings Settings;
		public float             AirControl;
		public float             AirControlResistance;
		public float             Drag;
	}

	public struct AirTime : IComponentData
	{
		public float Value;
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

	public struct SrtDodgeMovementComponent : IComponentData
	{
		public UTick DodgeQueued;
		
		public StaminaUsage StaminaUsage;

		public bool EnableGroundDodge, EnableAerialDodge;
	}

	public struct SrtWallBounceMovementComponent : IComponentData
	{
		public bool EnableWallDodge, EnableWallJump;

		public StaminaUsage StaminaUsageOnWallJump;
		public StaminaUsage StaminaUsageOnWallDodge;

		public UTick  LastBounceTick;
		public float3 LastBounceNormal;
	}

	public struct StaminaUsage
	{
		public bool  IsPercentage;
		public float Usage;

		public static StaminaUsage FromPercentage(float value)
		{
			StaminaUsage usage;
			usage.IsPercentage = true;
			usage.Usage        = value;
			return usage;
		}

		public static StaminaUsage FromAbsolute(float value)
		{
			StaminaUsage usage;
			usage.IsPercentage = false;
			usage.Usage        = value;
			return usage;
		}
	}

	public struct Stamina : IComponentData
	{
		public float Value;
		public float Max;
		public float GainPerSecond;

		public float GetAbsolute(StaminaUsage usage)
		{
			return usage.IsPercentage ? usage.Usage * Max : usage.Usage;
		}

		public bool HasEnough(StaminaUsage usage)
		{
			return GetAbsolute(usage) <= Value;
		}
		
		public bool HasEnough(StaminaUsage usage, out float neededPercentage)
		{
			if (GetAbsolute(usage) <= Value)
			{
				neededPercentage = 1;
				return true;
			}

			neededPercentage = Value / GetAbsolute(usage);
			return false;
		}

		public void Apply(StaminaUsage usage)
		{
			Value = math.max(Value - GetAbsolute(usage), 0);
		}
		
		public struct Exclude : IComponentData
		{}
		
		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<Stamina>
		{
			public uint Tick { get; set; }

			public int Value;
			public int Max;
			
			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedIntDelta(Value, baseline.Value, compressionModel);
				writer.WritePackedIntDelta(Max, baseline.Max, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Value = reader.ReadPackedIntDelta(ref ctx, baseline.Value, compressionModel);
				Max = reader.ReadPackedIntDelta(ref ctx, baseline.Max, compressionModel);
			}

			public void SynchronizeFrom(in Stamina component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Value = (int) (component.Value * 1000);
				Max = (int) (component.Max * 1000);
			}

			public void SynchronizeTo(ref Stamina component, in DeserializeClientData deserializeData)
			{
				component.Value = Value * 0.001f;
				component.Max = Max * 0.001f;
			}
		}
		
		public class Synchronize : ComponentSnapshotSystem_Basic<Stamina, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
		
		public class UpdateSystem : ComponentUpdateSystemDirect<Stamina, Snapshot>
		{}
	}
}