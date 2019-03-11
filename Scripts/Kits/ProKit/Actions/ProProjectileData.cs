using Unity.Entities;

namespace Scripts.Actions
{
	public enum StandardProjectilePhase : byte
	{
		None     = 0,
		Active   = 1,
		Exploded = 2
	}

	public struct ProProjectileData : IComponentData
	{
		public StandardProjectilePhase Phase;
		public int                     ExplodeTick;
	}
}