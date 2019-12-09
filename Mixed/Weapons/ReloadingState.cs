using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.Mixed
{
	public struct ReloadingState : IComponentData
	{
		public bool Active;

		public UTimeProgression Progress;
		public int              TimeToReload;
	}
}