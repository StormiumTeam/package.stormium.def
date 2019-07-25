using package.stormiumteam.networking.runtime.lowlevel;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Stormium.Default.Kits.ProKit.ProMortar
{
	public struct ProMortarAction : IComponentData, ISerializableAsPayload
	{
		public ProProjectile.Settings SpawnSettings;
		public float3                 Speed;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteRef(ref this);
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			this = data.ReadValue<ProMortarAction>();
		}
	}

	[DisableAutoCreation]
	public class ProMortarActionProcess : ActionBaseJobSystem
	{
		struct Job : IJobForEachWithEntity<ProMortarAction, ActionAmmo, ActionCooldown, StActionSlotInput, Owner>
		{
			public GameTime GameTime;

			[ReadOnly] public ComponentDataFromEntity<LocalToWorld> TransformFromEntity;
			[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookFromEntity;

			[NativeDisableParallelForRestriction] public NativeList<ProMortarProjectileProvider.Create> CreateProjectileList;

			public void Execute(Entity                entity, int _,
			                    ref ProMortarAction   mortar,
			                    ref ActionAmmo        ammo,
			                    ref ActionCooldown    cooldown,
			                    ref StActionSlotInput input,
			                    ref Owner             owner)
			{
				if (input.IsActive
				    && cooldown.CooldownFinished(GameTime.Tick)
				    && ammo.Value >= ammo.Usage)
				{
					cooldown.StartTick = GameTime.Tick;
					ammo.IncreaseFromDelta(-ammo.Value);

					new ShootHelper(TransformFromEntity[owner.Target], EyePositionFromEntity[owner.Target], AimLookFromEntity[owner.Target])
						.GetPositionAndDirection(out var shootPos, out var shootDir);

					CreateProjectileList.Add(new ProMortarProjectileProvider.Create
					{
						position = shootPos,
						velocity = shootDir * mortar.Speed,
						owner    = entity
					});
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

				CreateProjectileList = World.GetExistingSystem<ProMortarProjectileProvider>().GetEntityDelayedList()
			}.Schedule(this, jobHandle);
		}
	}

	public class ProMortarActionProvider : SystemProvider
	{
		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
		{
			excludedStreamerComponents = null;
			entityComponents = new ComponentType[]
			{
				typeof(ActionDescription),
				typeof(ActionTag),
				typeof(StActionSlotInput),
				typeof(ProMortarAction),
				typeof(ActionAmmo),
				typeof(ActionSlot),
				typeof(ActionCooldown),
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
			EntityManager.SetComponentData(action, new ProMortarAction
			{
				SpawnSettings = new ProProjectile.Settings
				{
					damageRadius = 1.5f,
					bumpRadius   = 1.6f,
					detectRadius = 0.1f,

					bumpForce = new float3(8)
				},
				Speed = 33.5f
			});

			return action;
		}
	}
}