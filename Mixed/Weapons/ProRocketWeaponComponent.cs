using System.Linq;
using Projectiles;
using Revolution;
using Stormium.Core;
using Stormium.Core.Projectiles;
using Stormium.Core.Weapon;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Mixed.Weapons
{
	public struct ProRocketWeaponCreate : IWeaponCreate<ProRocketWeaponComponent>
	{
		public Entity Owner { get; set; }
		public Entity Player { get; set; }
	}
	
	public struct ProRocketWeaponComponent : IComponentData
	{
		public class Synchronize : ComponentSnapshotSystemTag<ProRocketWeaponComponent>
		{}
	}

	public class ProRocketWeaponProvider : WeaponProviderBase<ProRocketWeaponComponent, ProRocketWeaponCreate>
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			base.GetComponents(out entityComponents);
			entityComponents = entityComponents.Concat(new ComponentType[]
			{
				typeof(ActionAmmo),
				typeof(ActionCooldown),
				typeof(ReloadingState)
			}).ToArray();
		}

		public override void SetEntityData(Entity entity, ProRocketWeaponCreate data)
		{
			base.SetEntityData(entity, data);
			EntityManager.SetComponentData(entity, new ActionAmmo(1, 3) {IsEnergyBased = false});
			EntityManager.SetComponentData(entity, new ActionCooldown {Cooldown        = 400});
			EntityManager.SetComponentData(entity, new ReloadingState {TimeToReload    = 1500});
		}
	}

	public class ProRocketWeaponSystem : WeaponSystemBase<ProRocketWeaponComponent>
	{
		private EntityQuery m_WeaponQuery;
		private EntityQuery m_ProjectileQuery;
		
		private static Entity s_OwnerTarget;

		protected override void Register(Entity desc)
		{
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_WeaponQuery = GetEntityQuery(typeof(ProRocketWeaponComponent), typeof(ActionAmmo), typeof(ActionCooldown), typeof(ReloadingState), typeof(Owner), typeof(Relative<PlayerDescription>));
			m_ProjectileQuery = GetEntityQuery(typeof(RocketProjectile), typeof(ProjectileDefaultExplosion), typeof(Owner), ComponentType.Exclude<ProjectileEndedTag>());
		}

		private void DestroyRocketsOf(Entity owner)
		{
			s_OwnerTarget = owner;
			
			Entities
				.With(m_ProjectileQuery)
				.ForEach((Entity e, ref Owner projOwner, ref Velocity velocity, ref ProjectileDefaultExplosion explosionData) =>
				{
					if (!EntityManager.Exists(projOwner.Target))
						return;

					if (projOwner.Target == s_OwnerTarget)
					{
						explosionData.DamageRadius         = 2.75f;
						explosionData.HorizontalImpulseMax = 8f;
						explosionData.VerticalImpulseMax   = 8f;
						explosionData.MaxDamage            = 30;

						EntityManager.AddComponent(e, typeof(ProjectileEndedTag));
						EntityManager.AddComponentData(e, new ProjectileExplodedEndReason {normal = math.normalizesafe(velocity.Value, math.up())});
					}
				});
		}

		protected override void OnUpdate()
		{
			Entities
				.With(m_WeaponQuery)
				.ForEach((Entity e, ref ProRocketWeaponComponent weapon, ref ActionAmmo ammo, ref ActionCooldown cooldown, ref ReloadingState reloading, ref Owner owner, ref Relative<PlayerDescription> relativePlayer) =>
			{
				var currentWeapon = EntityManager.GetComponentData<CurrentWeapon>(owner.Target);
				if (currentWeapon.Target != e)
				{
					reloading.Active = false;
					reloading.Progress.Reset();
					return;
				}

				var userCommand   = EntityManager.GetComponentData<GamePlayerUserCommand>(relativePlayer.Target);
				var actionCommand = EntityManager.GetBuffer<GamePlayerActionCommand>(relativePlayer.Target);
				if (actionCommand.Length < 2)
					return;

				var primary   = actionCommand[0];
				var secondary = actionCommand[1];

				// manage cooldown
				cooldown.Update(GetTick(true));
				// manage reload
				if (!cooldown.Active || cooldown.Progress.Value > 100)
					reloading.TriggerReload(userCommand.IsReloading, ammo.Value);
				
				if (reloading.UpdateReloadAndGetState(ammo.Value, primary.IsActive, GetTick(true)) == WeaponUtility.ReloadUpdateState.Finished)
				{
					ammo.Value = ammo.Max;
						
					cooldown.Progress.Reset();
					cooldown.Active = false;
				}

				// Destroy current rockets
				if (secondary.IsActive)
				{
					DestroyRocketsOf(e);
				}

				if (reloading.Active)
					return;

				if (primary.IsActive && !cooldown.Active && ammo.Value > 0)
				{
					cooldown.Progress.Reset();
					cooldown.Active = true;

					ammo.Value--;

					var projectileList = World.GetOrCreateSystem<RocketProjectile.Provider>().GetEntityDelayedList();
					var helper = new ActionShootHelper(EntityManager.GetComponentData<LocalToWorld>(owner.Target),
						new EyePosition(math.up() * 1.125f),
						EntityManager.GetComponentData<AimLookState>(owner.Target));

					projectileList.Add(new RocketProjectile.Create
					{
						Owner     = e,
						Position  = helper.GetPosition(),
						Direction = helper.GetDirectionWithAimDelta(float2.zero)
					});
				}
			});
		}
	}
}