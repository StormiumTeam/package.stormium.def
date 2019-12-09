using System;
using System.Reflection;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Rules
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	public class DefaultDamageRule : RuleBaseSystem
	{
		public struct Data : IComponentData
		{
			public float SelfDamageFactor;
			public float AlliesDamageFactor;
			public bool  DisableEventForNoDamage;
		}

		public override string Name        => "Default Damage Rule";
		public override string Description => "Automatically manage the damage events.";

		public RuleProperties<Data>                 CustomProperties;
		public RuleProperties<Data>.Property<float> SelfDamageFactorProperty;
		public RuleProperties<Data>.Property<float> AlliesDamageFactorProperty;
		public RuleProperties<Data>.Property<bool>  DisableEventForNoDamageProperty;

		private EntityQuery     m_EntityQuery;
		private EntityArchetype m_ModifyHealthArchetype;

		[BurstCompile]
		[RequireComponentTag(typeof(GameEvent))]
		struct JobCreateEvents : IJobForEachWithEntity<TargetDamageEvent>
		{
			public Data Data;

			public EntityCommandBuffer.Concurrent Ecb;
			public EntityArchetype                ModifyHealthArchetype;

			[ReadOnly]
			public ComponentDataFromEntity<Relative<TeamDescription>> TeamOwnerFromEntity;

			[ReadOnly]
			public BufferFromEntity<TeamAllies> AlliesFromTeam;

			public void Execute(Entity entity, int index, ref TargetDamageEvent damageEvent)
			{
				var shooterTeam = TeamOwnerFromEntity.Exists(damageEvent.Origin) ? TeamOwnerFromEntity[damageEvent.Origin].Target : default;
				var victimTeam  = TeamOwnerFromEntity.Exists(damageEvent.Destination) ? TeamOwnerFromEntity[damageEvent.Destination].Target : default;

				if (damageEvent.Origin == damageEvent.Destination && math.abs(Data.SelfDamageFactor) > math.FLT_MIN_NORMAL)
				{
					damageEvent.Damage = (int) math.round(damageEvent.Damage * Data.SelfDamageFactor);
				}
				else if (shooterTeam != default && victimTeam != default && shooterTeam == victimTeam && math.abs(Data.AlliesDamageFactor) > math.FLT_MIN_NORMAL)
				{
					damageEvent.Damage = (int) math.round(damageEvent.Damage * Data.AlliesDamageFactor);
				}

				//Debug.Log($"damage={damageEvent.Damage} to={damageEvent.Destination} from={damageEvent.Origin}");
				if (damageEvent.Damage == 0 && Data.DisableEventForNoDamage)
					return;

				var healthEvent = Ecb.CreateEntity(index, ModifyHealthArchetype);
				Ecb.SetComponent(index, healthEvent, new ModifyHealthEvent(ModifyHealthType.Add, damageEvent.Damage, damageEvent.Destination));
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ModifyHealthArchetype = EntityManager.CreateArchetype(typeof(ModifyHealthEvent));

			CustomProperties = AddRule<Data>(out var data);

			SelfDamageFactorProperty        = CustomProperties.Add("Self damage factor", t => t.SelfDamageFactor);
			AlliesDamageFactorProperty        = CustomProperties.Add("Allies damage factor", t => t.AlliesDamageFactor);
			DisableEventForNoDamageProperty = CustomProperties.Add("Disable event if no damage were dealt", t => t.DisableEventForNoDamage);

			SelfDamageFactorProperty.Value = 0.25f;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new JobCreateEvents
			{
				Data                  = GetSingleton<Data>(),
				Ecb                   = GetCommandBuffer().ToConcurrent(),
				ModifyHealthArchetype = m_ModifyHealthArchetype,
				TeamOwnerFromEntity   = GetComponentDataFromEntity<Relative<TeamDescription>>(),
				AlliesFromTeam = GetBufferFromEntity<TeamAllies>()
			}.Schedule(this, inputDeps);

			AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}