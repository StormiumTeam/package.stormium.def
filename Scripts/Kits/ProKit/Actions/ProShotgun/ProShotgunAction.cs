using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.Kits.ProKit.ProShotgun
{
	public struct ProShotgunAction : IComponentData
	{
		public ProProjectile.Settings SpawnSettings;
		public float3                 Speed;
		public int2                   Pattern;
		public float                  PatternSize;

		public void Shoot(ActionShootHelper shootHelper, NativeList<(float3 position, float3 velocity)> projectiles)
		{
			for (var x = 0; x != Pattern.x; x++)
			{
				for (var y = 0; y != Pattern.y; y++)
				{
					var shootPos = shootHelper.GetPosition();
					var shootDir = shootHelper.GetDirectionWithAimDelta(new float2(x - Pattern.x / 2, y - Pattern.y / 2) * PatternSize);

					projectiles.Add((shootPos, shootDir * Speed));
				}
			}
		}

		public struct Create
		{
			public int    Slot;
			public Entity Owner;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(ActionDescription),
					typeof(StActionSlotInput),
					typeof(ProShotgunAction),
					typeof(ActionAmmo),
					typeof(ActionSlot),
					typeof(ActionCooldown),
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.ReplaceOwnerData(entity, data.Owner);

				EntityManager.SetComponentData(entity, new ActionSlot(data.Slot));
				EntityManager.SetComponentData(entity, new ActionCooldown(default, 500));
				EntityManager.SetComponentData(entity, new ActionAmmo(1, 2)
				{
					IsEnergyBased  = false,
					ReloadPerRound = true,
					TimeToReload   = 1_100
				});
				EntityManager.SetComponentData(entity, new ProShotgunAction
				{
					SpawnSettings = new ProProjectile.Settings
					{
						damageRadius = 0.25f,
						bumpRadius   = 0.33f,
						detectRadius = 0.2f,

						damage = 2,

						bumpForce  = new float3(8),
						bounciness = new float3(1, 1, 1),
						maxBounce  = 2
					},
					Speed       = 200f,
					Pattern     = new int2(5, 5),
					PatternSize = 2f
				});
			}
		}
	}
}