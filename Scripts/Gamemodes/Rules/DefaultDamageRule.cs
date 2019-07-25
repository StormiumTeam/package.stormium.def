using Runtime.BaseSystems;
using Stormium.Default.States;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	public class DefaultDamageRule : RuleBaseSystem
	{
		public struct Data : IComponentData
		{
			public float SelfDamageFactor;
			public float SameTeamDamageFactor;
			public bool  DisableEventForNoDamage;
		}

		public override string Name        => "Default Damage Rule";
		public override string Description => "Automatically manage the damage events.";

		public RuleProperties<Data>                 CustomProperties;
		public RuleProperties<Data>.Property<float> SelfDamageFactorProperty;
		public RuleProperties<Data>.Property<float> TeamDamageFactorProperty;
		public RuleProperties<Data>.Property<bool>  DisableEventForNoDamageProperty;

		private EntityQuery     m_EntityQuery;
		private EntityArchetype m_ModifyHealthArchetype;

		[RequireComponentTag(typeof(GameEvent))]
		struct JobCreateEvents : IJobForEachWithEntity<TargetDamageEvent>
		{
			public Data Data;

			public EntityCommandBuffer.Concurrent Ecb;
			public EntityArchetype                ModifyHealthArchetype;

			public ComponentDataFromEntity<Relative<TeamDescription>> TeamOwnerFromEntity;

			public void Execute(Entity entity, int index, ref TargetDamageEvent damageEvent)
			{
				Debug.Log("origin: " + damageEvent.Damage);

				var shooterTeam = TeamOwnerFromEntity.Exists(damageEvent.Origin) ? TeamOwnerFromEntity[damageEvent.Origin].Target : default;
				var victimTeam  = TeamOwnerFromEntity.Exists(damageEvent.Destination) ? TeamOwnerFromEntity[damageEvent.Destination].Target : default;

				if (damageEvent.Origin == damageEvent.Destination && math.abs(Data.SelfDamageFactor) > math.FLT_MIN_NORMAL)
				{
					damageEvent.Damage = (int) math.round(damageEvent.Damage * Data.SelfDamageFactor);
				}
				else if (shooterTeam != default && victimTeam != default && shooterTeam == victimTeam && math.abs(Data.SameTeamDamageFactor) > math.FLT_MIN_NORMAL)
				{
					damageEvent.Damage = (int) math.round(damageEvent.Damage * Data.SameTeamDamageFactor);
				}

				Debug.Log("damage > " + damageEvent.Damage);

				if (damageEvent.Damage == 0 && Data.DisableEventForNoDamage)
					return;

				Debug.Log(damageEvent.Damage);

				var healthEvent = Ecb.CreateEntity(index, ModifyHealthArchetype);
				Ecb.SetComponent(index, healthEvent, new ModifyHealthEvent(ModifyHealthType.Add, damageEvent.Damage, damageEvent.Destination));
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ModifyHealthArchetype = EntityManager.CreateArchetype(typeof(ModifyHealthEvent));

			CustomProperties = AddRule<Data>(out var data);

			SelfDamageFactorProperty        = CustomProperties.Add("Self damage factor", ref data, ref data.SelfDamageFactor);
			TeamDamageFactorProperty        = CustomProperties.Add("Team damage factor", ref data, ref data.SameTeamDamageFactor);
			DisableEventForNoDamageProperty = CustomProperties.Add("Disable event if no damage were dealt", ref data, ref data.DisableEventForNoDamage);

			SelfDamageFactorProperty.Value = 0.25f;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new JobCreateEvents
			{
				Data                  = GetSingleton<Data>(),
				Ecb                   = GetCommandBuffer().ToConcurrent(),
				ModifyHealthArchetype = m_ModifyHealthArchetype,
				TeamOwnerFromEntity   = GetComponentDataFromEntity<Relative<TeamDescription>>()
			}.Schedule(this, inputDeps);

			AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}