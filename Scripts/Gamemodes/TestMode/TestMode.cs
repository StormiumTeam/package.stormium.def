using System;
using Runtime.BaseSystems;
using Stormium.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Ray = Unity.Physics.Ray;

namespace Scripts.Gamemodes.TestMode
{
	public unsafe class TestMode : GameModeSystem
	{
		private EntityQuery m_BumpedEntityQuery;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_BumpedEntityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(LivableDescription), typeof(LocalToWorld), typeof(PhysicsCollider)},
				Any = new ComponentType[] {typeof(Velocity), typeof(PhysicsVelocity)}
			});
		}

		protected override void OnUpdate()
		{
			World.GetExistingSystem<GameEventRuleSystemGroup>().Process();
		}
	}
}