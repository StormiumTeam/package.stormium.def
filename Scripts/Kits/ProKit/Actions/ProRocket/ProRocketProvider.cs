using System.Linq;
using StormiumTeam.GameBase;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using package.StormiumTeam.GameBase;
using Scripts.Actions.ProKitWeapons;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Stormium.Default
{
	public class ProRocketProjectileProvider : BaseProviderBatch<ProRocketProjectileProvider.Create>
	{
		public struct Create
		{
			public float3                 Position;
			public float3                 Velocity;
			public ProProjectile.Settings Settings;
			public Entity                 Owner;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = ProProjectile.ProviderBasicComponents
			                                .Append(typeof(ProRocketProjectile))
			                                .ToArray();
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new Translation {Value = data.Position});
			EntityManager.SetComponentData(entity, new Velocity(data.Velocity));
			EntityManager.SetComponentData(entity, data.Settings);
			EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase = StandardProjectilePhase.Active});
		}
	}

	public class ProRocketActionProvider : BaseProviderBatch<ProRocketActionProvider.Create>
	{
		public struct Create
		{
			public int                     Slot;
			public Entity                  Owner;
			public int?                    Cooldown;
			public ActionAmmo?             Ammo;
			public ProProjectile.Settings? Settings;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<ActionDescription>(),
				ComponentType.ReadWrite<ProRocketAction>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<ActionAmmo>(),
				ComponentType.ReadWrite<ActionSlot>(),
				ComponentType.ReadWrite<ActionCooldown>(),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new ActionSlot(data.Slot));
			EntityManager.SetComponentData(entity, new ActionCooldown(0, data.Cooldown ?? 350));
			EntityManager.SetComponentData(entity, data.Ammo ?? new ActionAmmo(1, 4)
			{
				IsEnergyBased  = false,
				ReloadPerRound = false,
				TimeToReload   = GameTime.Convert(2.3f)
			});
			EntityManager.SetComponentData(entity, new ProRocketAction
			{
				ProjectileSettings = data.Settings ?? new ProProjectile.Settings
				{
					damageRadius = 1.6f,
					bumpRadius   = 1.7f,
					detectRadius = 0.125f,

					bumpForce = new float3(6),
					damage    = 25
				}
			});
		}
	}

	public class ProRocketDetonateActionProvider : BaseProviderBatch<ProRocketDetonateActionProvider.Create>
	{
		public struct Create
		{
			public int    Slot;
			public Entity Owner;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				ComponentType.ReadWrite<ModelIdent>(),
				ComponentType.ReadWrite<ActionDescription>(),
				ComponentType.ReadWrite<ProRocketDetonateAction>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<ActionSlot>(),
				ComponentType.ReadWrite<ActionCooldown>(),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new ActionSlot(data.Slot));
			EntityManager.SetComponentData(entity, new ActionCooldown(0, 250));
		}
	}
}