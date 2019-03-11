using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Stormium.Default
{	
	public struct ProRocketProjectile : IComponentData
	{
		public float Radius;
		
		public class Streamer : SnapshotEntityDataAutomaticStreamer<ProRocketProjectile>
		{}
	}
}