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

namespace Stormium.Default.Mixed
{
	public struct ProRailgunWeaponCreate : IWeaponCreate<ProRailgunWeaponComponent>
	{
		public ProRailgunWeaponComponent Component;
		
		public Entity Owner { get; set; }
		public Entity Player { get; set; }
	}
	
	public struct ProRailgunWeaponComponent : IComponentData
	{
		public uint RailgunCooldown;
		public uint RailgunUsage;
		public uint BeamCooldown;

		public uint Reducer;

		// todo: Move it into ActionAmmo
		public UTimeProgression AmmoProgression;
	}

	public class ProRailgunWeaponProvider : WeaponProviderBase<ProRailgunWeaponComponent, ProRailgunWeaponCreate>
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

		public override void SetEntityData(Entity entity, ProRailgunWeaponCreate data)
		{
			base.SetEntityData(entity, data);
			EntityManager.SetComponentData(entity, new ProRailgunWeaponComponent
			{
				RailgunCooldown = data.Component.RailgunCooldown,
				RailgunUsage    = data.Component.RailgunUsage,
				BeamCooldown    = data.Component.BeamCooldown
			});
			EntityManager.SetComponentData(entity, new ActionAmmo(1, 100) {IsEnergyBased = true});
			EntityManager.SetComponentData(entity, new ActionCooldown {Cooldown          = 750});
			EntityManager.SetComponentData(entity, new ReloadingState {TimeToReload      = 1500});
		}
	}

	public class ProRailgunWeaponSystem : GameBaseSystem
	{
		private EntityQuery m_WeaponQuery;

		private static Entity s_OwnerTarget;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_WeaponQuery = GetEntityQuery(typeof(ProRailgunWeaponComponent), typeof(ActionAmmo), typeof(ActionCooldown), typeof(ReloadingState), typeof(Owner), typeof(Relative<PlayerDescription>));
		}

		protected override void OnUpdate()
		{
			Entities
				.With(m_WeaponQuery)
				.ForEach((Entity e, ref ProRailgunWeaponComponent weapon, ref ActionAmmo ammo, ref ActionCooldown cooldown, ref ReloadingState reloading, ref Owner owner, ref Relative<PlayerDescription> relativePlayer) =>
				{
					if (!EntityManager.Exists(owner.Target))
						return;
					
					if (!cooldown.Active && !reloading.Active)
					{
						weapon.AmmoProgression += GetTick(true);
						if (weapon.AmmoProgression.Value > 90 - math.min(weapon.Reducer, 40))
						{
							weapon.AmmoProgression.Value = 0;
							ammo.IncreaseFromDelta(1);

							weapon.Reducer++;
						}
					}

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
					reloading.TriggerReload(userCommand.IsReloading, ammo.Value);
					// can't cancel reloading with railgun
					if (reloading.UpdateReloadAndGetState(ammo.Value, false, GetTick(true)) == WeaponUtility.ReloadUpdateState.Finished)
					{
						ammo.Value = ammo.Max;
						
						weapon.AmmoProgression.Reset();
						cooldown.Progress.Reset();
						cooldown.Active = false;
					}

					if (reloading.Active)
						return;

					ammo.Usage = primary.IsActive ? (int) weapon.RailgunUsage : 1;

					var doShoot = !cooldown.Active && ammo.Value >= ammo.Usage && (primary.IsActive || secondary.IsActive);
					if (doShoot)
					{
						cooldown.Progress.Reset();
						cooldown.Active   = true;

						weapon.AmmoProgression.Reset();
						weapon.Reducer = 0;

						ammo.IncreaseFromDelta(-ammo.Usage);

						var provider = World.GetOrCreateSystem<RailgunScanner.Provider>();
						var helper = new ActionShootHelper(EntityManager.GetComponentData<LocalToWorld>(owner.Target),
							new EyePosition(math.up() * 1.125f),
							EntityManager.GetComponentData<AimLookState>(owner.Target));
						
						// RAILGUN
						if (primary.IsActive)
						{
							cooldown.Cooldown = (int) weapon.RailgunCooldown;
							var ent = provider.SpawnLocalEntityWithArguments(new RailgunScanner.Create
							{
								Owner     = e,
								Position  = helper.GetPosition(),
								Direction = helper.GetDirectionWithAimDelta(float2.zero),
								Scanner   = new RailgunScanner {MaxDistance = 75f, Radius = 0.21f},
								OnExplode = new ScannerDefaultExplosion
								{
									MaxDamage  = 35,
									MinDamage  = 20,
									ImpulseMax = 13f,
									ImpulseMin = 6.5f
								}
							});
							var dmgFallOf = EntityManager.AddBuffer<DistanceDamageFallOf>(ent);
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(1f, 1f));
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.72f, 0.75f));
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.75f, 0.5f));
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0f, 0f));

							var bumpFallOf = EntityManager.AddBuffer<DistanceImpulseFallOf>(ent);
							bumpFallOf.Add(DistanceImpulseFallOf.FromPercentage(1f, 1f));
							bumpFallOf.Add(DistanceImpulseFallOf.FromPercentage(0f, 0f));
						}
						// BEAMGUN
						else if (secondary.IsActive)
						{
							cooldown.Cooldown = (int) weapon.BeamCooldown;
							var ent = provider.SpawnLocalEntityWithArguments(new RailgunScanner.Create
							{
								Owner     = e,
								Position  = helper.GetPosition(),
								Direction = helper.GetDirectionWithAimDelta(float2.zero),
								Scanner   = new RailgunScanner {MaxDistance = 21, Radius = 0.1f},
								OnExplode = new ScannerDefaultExplosion
								{
									MaxDamage  = 3,
									MinDamage  = 1,
									ImpulseMax = 1.4f,
									ImpulseMin = 0.5f
								}
							});
							var dmgFallOf = EntityManager.AddBuffer<DistanceDamageFallOf>(ent);
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(1f, 1f)); // 3 damage
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(1f, 0.66f));
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.66f, 0.66f)); // 2 damage
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.66f, 0.33f));
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.33f, 0.33f)); // 1 damage 
							dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.33f, 0.0f));

							var bumpFallOf = EntityManager.AddBuffer<DistanceImpulseFallOf>(ent);
							bumpFallOf.Add(DistanceImpulseFallOf.FromPercentage(1f, 1f));
							bumpFallOf.Add(DistanceImpulseFallOf.FromPercentage(0f, 0f));
						}
					}
				});
		}
	}
}