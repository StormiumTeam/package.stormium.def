using System;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using UnityEngine;

namespace Stormium.Default.Actions.ProMinigun
{
	public struct ProMinigunAction
	{
		public struct PredictedState : IStateData, IComponentData
		{
			public byte  IsShooting;
			public float InShootingDuration;
		}

		public struct Settings : IComponentData
		{
			public float StartRadius,   EndRadius; // in meters
			public int   StartCooldown, EndCooldown;
			public float TransitionTime; // in seconds	
		}
	}

	[DisableAutoCreation]
	public class ProMinigunActionSystem : ActionBaseSystem<ProMinigunActionSystem.CreateProjectileRequest>
	{
		[Serializable]
		public struct CreateProjectileRequest
		{
			public float3 position;
			public float3 direction;
			public Entity action;
		}

		private EntityQuery                  m_Group;
		private ProMinigunProjectileProvider m_ProjectileProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Group = GetEntityQuery
			(
				ComponentType.ReadWrite<ProMinigunAction.Settings>(),
				ComponentType.ReadWrite<ProMinigunAction.PredictedState>(),
				ComponentType.ReadWrite<ActionAmmo>(),
				ComponentType.ReadWrite<ActionCooldown>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<EntityAuthority>()
			);
			m_ProjectileProvider = World.GetOrCreateSystem<ProMinigunProjectileProvider>();
		}

		protected override unsafe void OnActionUpdate()
		{
			using (var entityArray = m_Group.ToEntityArray(Allocator.TempJob))
			using (var settingsArray = m_Group.ToComponentDataArray<ProMinigunAction.Settings>(Allocator.TempJob))
			using (var stateArray = m_Group.ToComponentDataArray<ProMinigunAction.PredictedState>(Allocator.TempJob))
			using (var ammoArray = m_Group.ToComponentDataArray<ActionAmmo>(Allocator.TempJob))
			using (var cooldownArray = m_Group.ToComponentDataArray<ActionCooldown>(Allocator.TempJob))
			using (var slotInputArray = m_Group.ToComponentDataArray<StActionSlotInput>(Allocator.TempJob))
			using (var ownerArray = m_Group.ToComponentDataArray<Owner>(Allocator.TempJob))
			{
				for (var i = 0; i != entityArray.Length; i++)
				{
					ref var state    = ref UnsafeUtilityEx.ArrayElementAsRef<ProMinigunAction.PredictedState>(stateArray.GetUnsafePtr(), i);
					ref var ammo     = ref UnsafeUtilityEx.ArrayElementAsRef<ActionAmmo>(ammoArray.GetUnsafePtr(), i);
					ref var cooldown = ref UnsafeUtilityEx.ArrayElementAsRef<ActionCooldown>(cooldownArray.GetUnsafePtr(), i);

					Operate(entityArray[i], settingsArray[i], ref state, ref ammo, ref cooldown, slotInputArray[i], ownerArray[i]);
				}

				m_Group.CopyFromComponentDataArray(stateArray);
				m_Group.CopyFromComponentDataArray(ammoArray);
				m_Group.CopyFromComponentDataArray(cooldownArray);
			}
		}

		protected void Operate(in  Entity                          entity,
		                       in  ProMinigunAction.Settings       settings,
		                       ref ProMinigunAction.PredictedState state,
		                       ref ActionAmmo                      ammo,
		                       ref ActionCooldown                  cooldown,
		                       in  StActionSlotInput               input,
		                       in  Owner                           owner)
		{
			var spin = input.IsActive;
			var dt   = GetSingleton<GameTimeComponent>().DeltaTime;

			state.InShootingDuration = max(state.InShootingDuration + (spin ? dt : -dt), 0);
			if (!spin)
			{
				state.InShootingDuration = min(state.InShootingDuration, 1);
				state.IsShooting         = 0;

				ammo.IncreaseFromDelta(TickDelta);
			}
			else
			{
				// unlike other actions, we only remove a delta tick from the ammo value
				ammo.ModifyAmmo(ammo.Value - TickDelta);
				if (state.IsShooting == 0 && ammo.Value >= ammo.Usage)
				{
					state.IsShooting = 1;
				}

				if (ammo.Value <= 0)
					state.IsShooting = 0;
			}

			cooldown.Cooldown = (int) lerp(settings.StartCooldown, settings.EndCooldown, clamp(state.InShootingDuration / settings.TransitionTime, 0, 1));
			if (state.IsShooting == 1 && cooldown.CooldownFinished(Tick))
			{
				cooldown.StartTick = Tick;

				var angle  = Mathf.Deg2Rad * (state.InShootingDuration % 1 * 360);
				var radius = lerp(settings.StartRadius, settings.EndRadius, clamp(state.InShootingDuration / settings.TransitionTime, 0, 1));
				var offset = new float2(sin(angle), cos(angle)) * radius;

				GetPosition(in owner.Target, out var position);
				GetDirectionWithAimDelta(in owner.Target, in offset, out var direction);

				Debug.DrawRay(position, direction * 32f, Color.black, 1f);
				Debug.DrawRay(position, direction * 16f, Color.white, 0.1f);

				SpawnRequests.Add(new CreateProjectileRequest
				{
					position  = position,
					direction = direction * 200f,
					action    = entity
				});
			}
		}

		protected override void FinalizeSpawnRequests()
		{
			for (var i = 0; i != SpawnRequests.Length; i++)
			{
				var request = SpawnRequests[i];
				m_ProjectileProvider.SpawnLocal(request.position, request.direction, request.action);
			}
		}
	}
}