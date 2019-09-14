using Stormium.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Scripts.ActionBase
{
	public interface IShootEvent
	{
		Entity Target { get; set; }
	}

	public class DefaultActionBaseSystem<TAction, TFillJob, TShootEvent> : JobGameBaseSystem
		where TAction : struct, IComponentData
		where TFillJob : struct, DefaultActionBaseSystem<TAction, TFillJob, TShootEvent>.IFillJob
		where TShootEvent : struct, IShootEvent, IComponentData
	{
		public interface IFillJob
		{
			void Setup();
			void Shoot(TShootEvent ev, TAction action, Entity owner, ActionShootHelper sh);
		}

		[RequireComponentTag(typeof(ActionAutomatic))]
		private struct ProcessJob : IJobForEachWithEntity<TAction, ActionAmmo, ActionCooldown, StActionSlotInput, Owner>
		{
			public UTick Tick;

			[ReadOnly] public ComponentDataFromEntity<LocalToWorld> TransformFromEntity;
			[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookFromEntity;

			public TFillJob FillJob;

			public void Execute(Entity                entity, int _,
			                    ref TAction           action,
			                    ref ActionAmmo        ammo,
			                    ref ActionCooldown    cooldown,
			                    ref StActionSlotInput input,
			                    ref Owner             owner)
			{
				if (input.IsActive
				    && cooldown.CooldownFinished(Tick)
				    && ammo.Value >= ammo.Usage)
				{
					cooldown.StartTick = Tick;
					ammo.IncreaseFromDelta(-ammo.Usage);

					var sh = new ActionShootHelper(TransformFromEntity[owner.Target], EyePositionFromEntity[owner.Target], AimLookFromEntity[owner.Target]);

					FillJob.Shoot(default, action, owner.Target, sh);
				}

				ammo.IncreaseFromDelta(Tick.DeltaMs);
			}
		}

		private struct ShootJob : IJobForEach<TShootEvent>
		{
			[ReadOnly] public ComponentDataFromEntity<TAction> ActionSettingsFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Owner>   OwnerFromEntity;

			[ReadOnly] public ComponentDataFromEntity<LocalToWorld> TransformFromEntity;
			[ReadOnly] public ComponentDataFromEntity<EyePosition>  EyePositionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<AimLookState> AimLookFromEntity;

			public TFillJob FillJob;

			public void Execute(ref TShootEvent shootEvent)
			{
				var action = ActionSettingsFromEntity[shootEvent.Target];
				var owner  = OwnerFromEntity[shootEvent.Target];

				var sh = new ActionShootHelper(TransformFromEntity[owner.Target], EyePositionFromEntity[owner.Target], AimLookFromEntity[owner.Target]);

				FillJob.Shoot(shootEvent, action, owner.Target, sh);
			}
		}

		protected override JobHandle OnUpdate(JobHandle jobHandle)
		{
			var fillJob = new TFillJob();
			fillJob.Setup();

			jobHandle = new ProcessJob
			{
				Tick = ServerSimulationSystemGroup.GetTick(),

				TransformFromEntity   = GetComponentDataFromEntity<LocalToWorld>(),
				EyePositionFromEntity = GetComponentDataFromEntity<EyePosition>(),
				AimLookFromEntity     = GetComponentDataFromEntity<AimLookState>(),

				FillJob = fillJob
			}.Schedule(this, jobHandle);

			jobHandle = new ShootJob
			{
				ActionSettingsFromEntity = GetComponentDataFromEntity<TAction>(),
				OwnerFromEntity          = GetComponentDataFromEntity<Owner>(),

				TransformFromEntity   = GetComponentDataFromEntity<LocalToWorld>(),
				EyePositionFromEntity = GetComponentDataFromEntity<EyePosition>(),
				AimLookFromEntity     = GetComponentDataFromEntity<AimLookState>(),

				FillJob = fillJob
			}.Schedule(this, jobHandle);

			return jobHandle;
		}
	}
}