using package.stormiumteam.networking.runtime.lowlevel;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Kits.ProKit.ProShotgun
{
	public struct ProShotgunAction : IComponentData, ISerializableAsPayload
	{
		public ProProjectile.Settings SpawnSettings;
		public float3                 Speed;
		public int2                   Pattern;
		public float                  PatternSize;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteRef(ref this);
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			this = data.ReadValue<ProShotgunAction>();
		}

		public class Process : ActionBaseJobSystem
		{
			struct Job : IJobForEachWithEntity<ProShotgunAction, ActionAmmo, ActionCooldown, StActionSlotInput, Relative<MovableDescription>>
			{
				public GameTime GameTime;

				[ReadOnly] public ComponentDataFromEntity<LocalToWorld> TransformFromEntity;
				[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;
				[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookFromEntity;

				[NativeDisableParallelForRestriction] public NativeList<(float3 p, float3 v, Entity o)> CreateProjectileList;

				public void Execute(Entity                           entity, int _,
				                    ref ProShotgunAction             shotgun,
				                    ref ActionAmmo                   ammo,
				                    ref ActionCooldown               cooldown,
				                    ref StActionSlotInput            input,
				                    ref Relative<MovableDescription> movable)
				{
					if (input.IsActive
					    && cooldown.CooldownFinished(GameTime.Tick)
					    && ammo.Value >= ammo.Usage)
					{
						cooldown.StartTick = GameTime.Tick;
						ammo.IncreaseFromDelta(-ammo.Usage);

						var helper = new ShootHelper(TransformFromEntity[movable.Target], EyePositionFromEntity[movable.Target], AimLookFromEntity[movable.Target]);

						for (var x = 0; x != shotgun.Pattern.x; x++)
						{
							for (var y = 0; y != shotgun.Pattern.y; y++)
							{
								var shootPos = helper.GetPosition();
								var shootDir = helper.GetDirectionWithAimDelta(new float2(x - shotgun.Pattern.x / 2, y - shotgun.Pattern.y / 2) * shotgun.PatternSize);

								CreateProjectileList.Add((shootPos, shootDir * shotgun.Speed, entity));
							}
						}
					}

					ammo.IncreaseFromDelta(GameTime.DeltaTick);
				}
			}

			protected override JobHandle OnActionUpdate(JobHandle jobHandle)
			{
				return new Job
				{
					GameTime              = GetSingleton<GameTimeComponent>().ToGameTime(),
					TransformFromEntity   = GetComponentDataFromEntity<LocalToWorld>(),
					AimLookFromEntity     = GetComponentDataFromEntity<AimLookState>(),
					EyePositionFromEntity = GetComponentDataFromEntity<EyePosition>(),

					CreateProjectileList = World.GetExistingSystem<ProShotgunProjectile.Provider>().GetEntityDelayedList()
				}.Schedule(this, jobHandle);
			}
		}

		public class Provider : SystemProvider
		{
			public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
			{
				excludedStreamerComponents = null;
				entityComponents = new ComponentType[]
				{
					typeof(ActionDescription),
					typeof(ActionTag),
					typeof(StActionSlotInput),
					typeof(ProShotgunAction),
					typeof(ActionAmmo),
					typeof(ActionSlot),
					typeof(ActionCooldown),
					typeof(GenerateEntitySnapshot)
				};
			}

			public Entity SpawnLocal(Entity owner, int slot)
			{
				var action = SpawnLocal();

				var c = EntityManager.GetComponentTypes(action);
				foreach (var component in c)
				{
					Debug.Log(component.GetManagedType().Name);
				}
				c.Dispose();

				EntityManager.ReplaceOwnerData(action, owner);

				EntityManager.SetComponentData(action, new ActionSlot(slot));
				EntityManager.SetComponentData(action, new ActionCooldown(0, 600));
				EntityManager.SetComponentData(action, new ActionAmmo(1500, 3000));
				EntityManager.SetComponentData(action, new ProShotgunAction
				{
					SpawnSettings = new ProProjectile.Settings
					{
						damageRadius = 1.5f,
						bumpRadius   = 1.6f,
						detectRadius = 0.1f,

						damage = 4,

						bumpForce  = new float3(8),
						bounciness = new float3(1, 1, 1),
						maxBounce  = 2
					},
					Speed       = 200f,
					Pattern     = new int2(5, 5),
					PatternSize = 2.75f
				});

				return action;
			}
		}
	}
}