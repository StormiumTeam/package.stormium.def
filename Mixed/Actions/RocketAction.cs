using Unity.NetCode;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Actions
{
	public struct RocketAction : IComponentData
	{
		public bool EnableSecondary;

		public class Process : GameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((ref RocketAction action, ref ActionAmmo actionAmmo, ref Relative<PlayerDescription> player) => { });
			}
		}
	}
}