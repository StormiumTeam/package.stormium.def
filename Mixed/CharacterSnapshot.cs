using CharacterController;
using package.stormiumteam.shared.ecs;
using Revolution;
using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Physics;
using Unity.Transforms;

namespace ProKit
{
	public struct CharacterComponent : IComponentData
	{
	}
	
	public struct CharacterSnapshot : IReadWriteSnapshot<CharacterSnapshot>
	{
		public void WriteTo(DataStreamWriter writer, ref CharacterSnapshot baseline, NetworkCompressionModel compressionModel)
		{

		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref CharacterSnapshot baseline, NetworkCompressionModel compressionModel)
		{

		}

		public uint Tick { get; set; }
	}

	public class CharacterNetworkSynchronize : EntitySerializer<CharacterNetworkSynchronize, CharacterSnapshot, CharacterNetworkSynchronize.SharedData>
	{
		public struct Exclude : IComponentData
		{

		}

		public struct SharedData
		{

		}

		public override NativeArray<ComponentType> EntityComponents =>
			new NativeArray<ComponentType>(3, Allocator.Temp)
			{
				[0] = typeof(CharacterComponent),
				[1] = typeof(PhysicsCollider),
				[2] = typeof(PhysicsCharacter),
			};

		public override ComponentType ExcludeComponent => typeof(Exclude);

		private static void OnSerialize(ref SerializeParameters parameters)
		{

		}

		private void OnDeserialize(ref DeserializeParameters parameters)
		{
			
		}

		protected override void GetDelegates(out BurstDelegate<OnSerializeSnapshot> onSerialize, out BurstDelegate<OnDeserializeSnapshot> onDeserialize)
		{
			onSerialize   = new BurstDelegate<OnSerializeSnapshot>(OnSerialize);
			onDeserialize = new BurstDelegate<OnDeserializeSnapshot>(OnDeserialize);
		}

		public override void OnBeginSerialize(Entity entity)
		{

		}

		public override void OnBeginDeserialize(Entity entity)
		{

		}
	}
}