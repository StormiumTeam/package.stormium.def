using package.stormiumteam.shared;
using Revolution;
using Unity.NetCode;
using Revolution.Utils;
using Stormium.Core;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace CharacterController
{
	public struct CharacterInput : IComponentData
	{
		public float2 Move;
		public float2 Look;
		public bool   Jump;
		public bool   Dodge;
		public bool   Crouch;

		public struct Exclude : IComponentData
		{
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<CharacterInput>
		{
			public QuantizedFloat2 Move;
			public QuantizedFloat2 Look;
			public bool            Jump;
			public bool            Dodge;
			public bool            Crouch;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				for (var i = 0; i != 2; i++)
				{
					writer.WritePackedIntDelta(Move[i], baseline.Move[i], compressionModel);
					writer.WritePackedIntDelta(Look[i], baseline.Look[i], compressionModel);
				}

				var pos  = 0;
				var mask = default(byte);
				MainBit.SetBitAt(ref mask, pos++, Jump);
				MainBit.SetBitAt(ref mask, pos++, Dodge);
				MainBit.SetBitAt(ref mask, pos++, Crouch);

				pos = 0;
				var baselineMask = default(byte);
				MainBit.SetBitAt(ref baselineMask, pos++, baseline.Jump);
				MainBit.SetBitAt(ref baselineMask, pos++, baseline.Dodge);
				MainBit.SetBitAt(ref baselineMask, pos++, baseline.Crouch);

				writer.WritePackedUIntDelta(mask, baselineMask, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				for (var i = 0; i != 2; i++)
				{
					Move[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Move[i], compressionModel);
					Look[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Look[i], compressionModel);
				}

				var pos          = 0;
				var baselineMask = default(byte);
				MainBit.SetBitAt(ref baselineMask, pos++, baseline.Jump);
				MainBit.SetBitAt(ref baselineMask, pos++, baseline.Dodge);
				MainBit.SetBitAt(ref baselineMask, pos++, baseline.Crouch);

				pos = 0;
				var mask = reader.ReadPackedUIntDelta(ref ctx, baselineMask, compressionModel);
				Jump   = MainBit.GetBitAt(mask, pos++) == 1;
				Dodge  = MainBit.GetBitAt(mask, pos++) == 1;
				Crouch = MainBit.GetBitAt(mask, pos++) == 1;
			}

			public uint Tick { get; set; }

			public void SynchronizeFrom(in CharacterInput component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Move.Set(100, component.Move);
				Look.Set(100, component.Look);
				Jump   = component.Jump;
				Dodge  = component.Dodge;
				Crouch = component.Crouch;
			}

			public void SynchronizeTo(ref CharacterInput component, in DeserializeClientData deserializeData)
			{
				component.Move   = Move.Get(0.01f);
				component.Look   = Look.Get(0.01f);
				component.Jump   = Jump;
				component.Dodge  = Dodge;
				component.Crouch = Crouch;
			}
		}

		public class Synchronize : ComponentSnapshotSystemBasic<CharacterInput, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<CharacterInput, Snapshot>
		{
		}
	}

	[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class CharacterControllerSetInput : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((ref LocalToWorld ltw, ref CharacterInput input, ref AimLookState aimLookState, ref Relative<PlayerDescription> playerRelative) =>
			{
				if (!EntityManager.HasComponent<GamePlayerUserCommand>(playerRelative.Target) || !EntityManager.HasComponent<WorldOwnedTag>(playerRelative.Target))
					return;

				var userCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(playerRelative.Target);
				input.Look   = userCommand.Look;
				input.Move   = userCommand.Move;
				input.Jump   = userCommand.IsJumping;
				input.Dodge  = userCommand.IsDodging;
				input.Crouch = userCommand.IsCrouching;

				aimLookState.Aim = input.Look;
			});

			Entities.ForEach((ref StandardJumpMovement component, ref Relative<PlayerDescription> playerRelative) =>
			{
				if (!EntityManager.HasComponent<GamePlayerUserCommand>(playerRelative.Target) || !EntityManager.HasComponent<WorldOwnedTag>(playerRelative.Target))
					return;

				var userCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(playerRelative.Target);
				if (userCommand.QueueJump)
				{
					component.JumpQueued = UTick.AddTick(GetTick(true), 2);
				}
			});
			Entities.ForEach((ref StandardDodgeMovement component, ref Relative<PlayerDescription> playerRelative) =>
			{
				if (!EntityManager.HasComponent<GamePlayerUserCommand>(playerRelative.Target) || !EntityManager.HasComponent<WorldOwnedTag>(playerRelative.Target))
					return;

				var userCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(playerRelative.Target);
				if (userCommand.QueueDodge)
				{
					component.DodgeQueued = UTick.AddTick(GetTick(true), 4);
				}
			});
		}
	}
}