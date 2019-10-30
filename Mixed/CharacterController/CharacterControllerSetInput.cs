using Revolution;
using Revolution.NetCode;
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
			public uint            Jump;
			public uint            Dodge;
			public uint            Crouch;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				for (var i = 0; i != 2; i++)
				{
					writer.WritePackedIntDelta(Move[i], baseline.Move[i], compressionModel);
					writer.WritePackedIntDelta(Look[i], baseline.Look[i], compressionModel);
				}

				writer.WritePackedUIntDelta(Jump, baseline.Jump, compressionModel);
				writer.WritePackedUIntDelta(Dodge, baseline.Dodge, compressionModel);
				writer.WritePackedUIntDelta(Crouch, baseline.Crouch, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				for (var i = 0; i != 2; i++)
				{
					Move[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Move[i], compressionModel);
					Look[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Look[i], compressionModel);
				}

				Jump   = reader.ReadPackedUIntDelta(ref ctx, baseline.Jump, compressionModel);
				Dodge  = reader.ReadPackedUIntDelta(ref ctx, baseline.Dodge, compressionModel);
				Crouch = reader.ReadPackedUIntDelta(ref ctx, baseline.Crouch, compressionModel);
			}

			public uint Tick { get; set; }

			public void SynchronizeFrom(in CharacterInput component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Move.Set(1000, component.Move);
				Look.Set(1000, component.Look);
				Jump   = component.Jump ? 1u : 0u;
				Dodge  = component.Dodge ? 1u : 0u;
				Crouch = component.Crouch ? 1u : 0u;
			}

			public void SynchronizeTo(ref CharacterInput component, in DeserializeClientData deserializeData)
			{
				component.Move   = Move.Get(0.001f);
				component.Look   = Look.Get(0.001f);
				component.Jump   = Jump == 1;
				component.Dodge  = Dodge == 1;
				component.Crouch = Crouch == 1;
			}
		}

		public class Synchronize : ComponentSnapshotSystem_Basic<CharacterInput, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<CharacterInput, Snapshot>
		{
		}
	}

	[UpdateInGroup(typeof(PredictionSimulationSystemGroup))]
	[UpdateInWorld(WorldType.ServerWorld)]
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

				Debug.DrawRay(ltw.Position, Vector3.up * 2, Color.red);
				Debug.DrawRay(ltw.Position, ltw.Forward, Color.green);
			});
			
			Entities.ForEach((ref SrtJumpMovementComponent component, ref Relative<PlayerDescription> playerRelative) =>
			{
				if (!EntityManager.HasComponent<GamePlayerUserCommand>(playerRelative.Target) || !EntityManager.HasComponent<WorldOwnedTag>(playerRelative.Target))
					return;

				var userCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(playerRelative.Target);
				if (userCommand.QueueJump)
				{
					component.JumpQueued = UTick.AddTick(GetTick(true), 2);
				}
			});
			Entities.ForEach((ref SrtDodgeMovementComponent component, ref Relative<PlayerDescription> playerRelative) =>
			{
				if (!EntityManager.HasComponent<GamePlayerUserCommand>(playerRelative.Target) || !EntityManager.HasComponent<WorldOwnedTag>(playerRelative.Target))
					return;

				var userCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(playerRelative.Target);
				if (userCommand.QueueDodge)
				{
					component.DodgeQueued = UTick.AddTick(GetTick(true), 2);
				}
			});
		}
	}
}