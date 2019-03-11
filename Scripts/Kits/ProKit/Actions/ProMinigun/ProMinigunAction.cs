using System;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumShared.Core.Networking;
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
			public float StartRadius, EndRadius; // in meters
			public float TransitionTime;         // in seconds	
		}
	}

	[UpdateInGroup(typeof(ProActionSystemGroup))]
	public class ProMinigunSystem : ActionBaseSystem<ProMinigunSystem.CreateProjectileRequest>
	{
		[Serializable]
		public struct CreateProjectileRequest
		{
			public float3 position;
			public float3 direction;
			public Entity action;
		}

		private ComponentGroup     m_Group;
		private ProMinigunProjectileProvider m_ProjectileProvider;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_Group = GetComponentGroup
			(
				ComponentType.ReadWrite<ProMinigunAction.Settings>(),
				ComponentType.ReadWrite<ProMinigunAction.PredictedState>(),
				ComponentType.ReadWrite<ActionAmmo>(),
				ComponentType.ReadWrite<ActionCooldown>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<OwnerState<LivableDescription>>(),
				ComponentType.ReadWrite<EntityAuthority>()
			);
			m_ProjectileProvider = World.GetExistingManager<ProMinigunProjectileProvider>();
		}

		protected override unsafe void OnActionUpdate()
		{
			using (var entityArray = m_Group.ToEntityArray(Allocator.TempJob))
			using (var settingsArray = m_Group.ToComponentDataArray<ProMinigunAction.Settings>(Allocator.TempJob))
			using (var stateArray = m_Group.ToComponentDataArray<ProMinigunAction.PredictedState>(Allocator.TempJob))
			using (var ammoArray = m_Group.ToComponentDataArray<ActionAmmo>(Allocator.TempJob))
			using (var cooldownArray = m_Group.ToComponentDataArray<ActionCooldown>(Allocator.TempJob))
			using (var slotInputArray = m_Group.ToComponentDataArray<StActionSlotInput>(Allocator.TempJob))
			using (var ownerArray = m_Group.ToComponentDataArray<OwnerState<LivableDescription>>(Allocator.TempJob))
			{
				for (var i = 0; i != entityArray.Length; i++)
				{
					var state    = UnsafeUtilityEx.ArrayElementAsRef<ProMinigunAction.PredictedState>(settingsArray.GetUnsafePtr(), i);
					var ammo     = UnsafeUtilityEx.ArrayElementAsRef<ActionAmmo>(ammoArray.GetUnsafePtr(), i);
					var cooldown = UnsafeUtilityEx.ArrayElementAsRef<ActionCooldown>(cooldownArray.GetUnsafePtr(), i);

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
		                       ref ActionAmmo                    ammo,
		                       ref ActionCooldown                cooldown,
		                       in  StActionSlotInput               input,
		                       in  OwnerState<LivableDescription>  owner)
		{
			if (input.IsActive && cooldown.CooldownFinished(Tick) && ammo.Value >= ammo.Usage)
			{
				ammo.ModifyAmmo(ammo.Value - ammo.Usage);
				cooldown.StartTick = Tick;

				var angle  = Mathf.Deg2Rad * (state.InShootingDuration % 1 * 360);
				var radius = lerp(settings.StartRadius, settings.EndRadius, clamp(state.InShootingDuration / settings.TransitionTime, 0, 1));
				var offset = new float2(sin(angle), cos(angle)) * radius;

				GetPosition(in owner.Target, out var position);
				GetDirectionWithAimDelta(in owner.Target, in offset, out var direction);

				SpawnRequests.Add(new CreateProjectileRequest
				{
					position  = position,
					direction = direction,
					action = entity
				});
			}

			ammo.IncreaseFromDelta(TickDelta);
		}

		protected override void FinalizeSpawnRequests()
		{
			foreach (var request in SpawnRequests)
			{
				m_ProjectileProvider.SpawnLocal(request.position, request.direction, request.action);
			}
		}
	}
}