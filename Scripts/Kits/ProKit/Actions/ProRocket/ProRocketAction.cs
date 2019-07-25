using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default
{
	public struct ProRocketAction : IStateData, IComponentData
	{
		public ProProjectile.Settings ProjectileSettings;
		public int                    LastShoot;
	}

	[DisableAutoCreation]
	public class ProRocketActionUpdateSystem : ActionBaseJobSystem
	{
		public const float ProjSpeed = 30f;

		private struct JobUpdateAction : IJobForEachWithEntity<ProRocketAction, ActionAmmo, ActionCooldown, StActionSlotInput, Relative<MovableDescription>>
		{
			public int Tick;
			public int TickDelta;

			[NativeDisableParallelForRestriction] public NativeList<ProRocketProjectileProvider.Create> CreateList;

			[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<LocalToWorld> LocalToWorldFromEntity;
			[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;

			public void Execute(Entity                                      entity, int index,
			                    ref            ProRocketAction              action,
			                    ref            ActionAmmo                   ammo,
			                    ref            ActionCooldown               cooldown,
			                    [ReadOnly] ref StActionSlotInput            input,
			                    [ReadOnly] ref Relative<MovableDescription> movable)
			{
				if (input.IsActive && cooldown.CooldownFinished(Tick) && ammo.Value >= ammo.Usage)
				{
					// Restart cooldown...
					cooldown.StartTick = Tick;
					ammo.ModifyAmmo(ammo.Value - ammo.Usage);

					var aim = AimLookStateFromEntity[movable.Target];
					var pos = LocalToWorldFromEntity[movable.Target].Position + EyePositionFromEntity[movable.Target].Value;
					var fwd = Quaternion.Euler(-aim.Aim.y, aim.Aim.x, 0) * Vector3.forward;

					CreateList.Add(new ProRocketProjectileProvider.Create
					{
						Position = pos,
						Velocity = fwd * ProjSpeed,
						Settings = action.ProjectileSettings,
						Owner    = entity,
					});

					action.LastShoot = Tick;
				}

				ammo.IncreaseFromDelta(TickDelta);
			}
		}

		private EntityQuery m_ActionGroup;

		protected override void OnStartRunning()
		{
			var query = new EntityQueryDesc
			{
				All = new[]
				{
					ComponentType.ReadWrite<ProRocketAction>(), ComponentType.ReadWrite<ActionAmmo>(), ComponentType.ReadWrite<ActionCooldown>(),
					ComponentType.ReadOnly<StActionSlotInput>(), ComponentType.ReadOnly<Relative<MovableDescription>>(), ComponentType.ReadOnly<EntityAuthority>()
				}
			};
			m_ActionGroup = GetEntityQuery(query);
		}

		protected override JobHandle OnActionUpdate(JobHandle jobHandle)
		{
			var job = new JobUpdateAction
			{
				Tick      = Tick,
				TickDelta = TickDelta,

				CreateList = World.GetExistingSystem<ProRocketProjectileProvider>().GetEntityDelayedList(),

				AimLookStateFromEntity = GetComponentDataFromEntity<AimLookState>(true),
				LocalToWorldFromEntity = GetComponentDataFromEntity<LocalToWorld>(true),
				EyePositionFromEntity  = GetComponentDataFromEntity<EyePosition>(true),
			};

			return JobForEachExtensions.Schedule(job, m_ActionGroup, jobHandle);
		}
	}
}