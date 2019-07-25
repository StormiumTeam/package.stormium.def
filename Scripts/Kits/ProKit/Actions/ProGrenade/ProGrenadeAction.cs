using package.stormiumteam.networking.runtime.lowlevel;
using package.StormiumTeam.GameBase;
using Scripts.ActionBase;
using Stormium.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.Kits.ProKit.ProGrenade
{
	public struct ProGrenadeAction : IComponentData, ISerializableAsPayload
	{
		public struct ShootEvent : IComponentData, IShootEvent
		{
			public Entity Target { get; set; }
		}
		
		public ProProjectile.Settings SpawnSettings;
		public float3 Speed;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteRef(ref this);
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			this = data.ReadValue<ProGrenadeAction>();
		}
	}

	[DisableAutoCreation]
	public class ProGrenadeActionProcess : DefaultActionBaseSystem<ProGrenadeAction, ProGrenadeActionProcess.FillJob, ProGrenadeAction.ShootEvent>
	{
		public struct FillJob : IFillJob
		{
			[NativeDisableParallelForRestriction] public NativeList<(float3 p, float3 v, ProProjectile.Settings? s, Entity o)> CreateList;

			public void Setup()
			{
				CreateList = World.Active.GetExistingSystem<ProGrenadeProjectile.Provider>().GetEntityDelayedList();
			}

			public void Shoot(ProGrenadeAction.ShootEvent ev, ProGrenadeAction action, Entity owner, ShootHelper sh)
			{
				sh.GetPositionAndDirection(out var pos, out var dir);

				CreateList.Add((pos, dir * action.Speed, null, owner));
			}
		}

		/*[RequireComponentTag(typeof(ActionAutomatic))]
		struct Job : IJobForEachWithEntity<ProGrenadeAction, ActionAmmo, ActionCooldown, StActionSlotInput, OwnerState<MovableDescription>>
		{
			public GameTime GameTime;

			[ReadOnly] public ComponentDataFromEntity<LocalToWorld> TransformFromEntity;
			[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookFromEntity;

			[NativeDisableParallelForRestriction] public NativeList<(float3 p, float3 v, ProProjectile.Settings? s, Entity o)> CreateProjectileList;

			public void Execute(Entity                             entity, int _,
			                    ref ProGrenadeAction               grenade,
			                    ref ActionAmmo                     ammo,
			                    ref ActionCooldown                 cooldown,
			                    ref StActionSlotInput              input,
			                    ref OwnerState<MovableDescription> movable)
			{
				if (input.IsActive
				    && cooldown.CooldownFinished(GameTime.Tick)
				    && ammo.Value >= ammo.Usage)
				{
					cooldown.StartTick = GameTime.Tick;
					ammo.IncreaseFromDelta(-ammo.Usage);

					new ShootHelper(TransformFromEntity[movable.Target], EyePositionFromEntity[movable.Target], AimLookFromEntity[movable.Target])
						.GetPositionAndDirection(out var shootPos, out var shootDir);

					CreateProjectileList.Add((shootPos, shootDir * grenade.Speed, null, movable.Target));
				}

				ammo.IncreaseFromDelta(GameTime.DeltaTick);
			}
		}

		[ExcludeComponent(typeof(ActionAutomatic))]
		struct ShootJob : IJobForEach<ProGrenadeAction.ShootEvent>
		{
			[ReadOnly] public ComponentDataFromEntity<ProGrenadeAction>               ActionSettingsFromEntity;
			[ReadOnly] public ComponentDataFromEntity<OwnerState<MovableDescription>> MovableOwnerFromEntity;

			[ReadOnly] public ComponentDataFromEntity<LocalToWorld> TransformFromEntity;
			[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookFromEntity;

			[NativeDisableParallelForRestriction] public NativeList<ProGrenadeProjectileProvider.Create> CreateProjectileList;

			public void Execute(ref ProGrenadeAction.ShootEvent shootEvent)
			{
				var grenade = ActionSettingsFromEntity[shootEvent.Target];
				var owner   = MovableOwnerFromEntity[shootEvent.Target];

				new ShootHelper(TransformFromEntity[owner.Target], EyePositionFromEntity[owner.Target], AimLookFromEntity[owner.Target])
					.GetPositionAndDirection(out var shootPos, out var shootDir);

				CreateProjectileList.Add(new ProGrenadeProjectileProvider.Create
				{
					position = shootPos,
					velocity = shootDir * grenade.Speed,
					owner    = shootEvent.Target
				});
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

				CreateProjectileList = World.GetExistingSystem<ProGrenadeProjectile.Provider>().GetEntityDelayedList()
			}.Schedule(this, jobHandle);
		}*/
	}

	public class ProGrenadeActionProvider : SystemProvider
	{
		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
		{
			excludedStreamerComponents = null;
			entityComponents = new ComponentType[]
			{
				typeof(ActionDescription),
				typeof(ActionTag),
				typeof(StActionSlotInput),
				typeof(ProGrenadeAction),
				typeof(ActionAmmo),
				typeof(ActionSlot),
				typeof(ActionCooldown),
				typeof(ActionAutomatic),
				typeof(GenerateEntitySnapshot)
			};
		}

		public Entity SpawnLocal(Entity owner, int slot)
		{
			var action = SpawnLocal();

			EntityManager.ReplaceOwnerData(action, owner);

			EntityManager.SetComponentData(action, new ActionSlot(slot));
			EntityManager.SetComponentData(action, new ActionCooldown(0, 400));
			EntityManager.SetComponentData(action, new ActionAmmo(2000, 4000));
			EntityManager.SetComponentData(action, new ProGrenadeAction
			{
				SpawnSettings = new ProProjectile.Settings
				{
					damageRadius = 1.5f,
					bumpRadius   = 1.6f,
					detectRadius = 0.2f,

					bumpForce = new float3(8)
				},
				Speed = 30f
			});

			return action;
		}
	}
}