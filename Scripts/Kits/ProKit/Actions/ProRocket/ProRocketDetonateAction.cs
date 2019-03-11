using Stormium.Core;
using Stormium.Default;
using Stormium.Default.Kits.ProKit;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Scripts.Actions.ProKitWeapons
{
	public struct ProRocketDetonateAction : IStateData, IComponentData
	{
		public class Streamer : SnapshotEntityDataAutomaticStreamer<ProRocketDetonateAction>
		{
		}

		public float cooldown;
	}

	[UpdateInGroup(typeof(ProActionSystemGroup))]
	public class ProRocketDetonateActionUpdateSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			ForEach((ref ProRocketDetonateAction detonateAction, ref StActionSlotInput input, ref OwnerState<LivableDescription> ownerCharacterRef) =>
			{
				if (!input.IsActive)
					return;

				var ownerCharacter = ownerCharacterRef;
				ForEach((ref ProRocketProjectile rocket, ref ProProjectileData projectile, ref OwnerState<LivableDescription> rocketOwnerCharacter) =>
				{
					if (rocketOwnerCharacter.Target != ownerCharacter.Target)
						return;

					projectile.ExplodeTick = Tick;
				});
			});
		}
	}
}