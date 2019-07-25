using System;
using System.Collections.Generic;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Scripts.Bumpers
{
	[Serializable]
	public struct LaunchPad : IComponentData
	{
		public float3 direction;
		public float3 reset;
		public float force;
	}

	public unsafe class LaunchPadSystem : GameBaseSystem
	{
		private struct ValuePadCooldown
		{
			public Entity PadEntity;
			public int LastTick;
		}

		private EntityQuery m_LaunchPadQuery;
		private EntityQuery m_MovableQuery;
		
		// TODO: Replace this with a buffer on the pad entity instead
		private Dictionary<Entity, ValuePadCooldown> m_Cooldowns;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Cooldowns = new Dictionary<Entity, ValuePadCooldown>();
			m_LaunchPadQuery = Entities.WithAll<LocalToWorld, LaunchPad>().ToEntityQuery();
			m_MovableQuery = Entities.WithAll<LocalToWorld, LivableDescription>().ToEntityQuery();
		}

		public void OnBump(Entity padEntity,     LocalToWorld padTransform,     LaunchPad       pad, PhysicsCollider padCollider,
		                   Entity movableEntity, LocalToWorld movableTransform, PhysicsCollider movableCollider)
		{
			var padRigidTransform     = new RigidTransform(padTransform.Value);
			var movableRigidTransform = new RigidTransform(movableTransform.Value);

			if (!padCollider.ColliderPtr->CalculateAabb(padRigidTransform)
				.Overlaps(movableCollider.ColliderPtr->CalculateAabb(movableRigidTransform)))
				return;

			var cwAgainst  = new CustomCollide(movableCollider, movableTransform);
			var collection = new CustomCollideCollection(&cwAgainst);
			var penetrateInput = new ColliderDistanceInput
			{
				Collider    = padCollider.ColliderPtr,
				MaxDistance = 0f,
				Transform   = padRigidTransform
			};

			var anyCollector = new AnyHitCollector<DistanceHit>(0.0f);
			if (!collection.CalculateDistance(penetrateInput, ref anyCollector))
				return;

			// Create Bump event
			Debug.Log("Bump!");

			var canBump = !m_Cooldowns.TryGetValue(movableEntity, out var cooldown) // if the value don't exist in the dictionary, it's ok
			              || cooldown.LastTick + 100 < Tick                         // if the cooldown is over, it's ok
			              || cooldown.PadEntity != padEntity;                       // if the last triggered pad isn't the same, it's ok

			if (!canBump)
				return;

			m_Cooldowns[movableEntity] = new ValuePadCooldown
			{
				PadEntity = padEntity,
				LastTick  = GameTime.Tick
			};

			var provider = World.Active.GetExistingSystem<LaunchPadBumpEventProvider>();
			var delayList = provider.GetEntityDelayedList();
			
			delayList.Add(new LaunchPadBumpEventProvider.Create
			{
				data = new TargetBumpEvent
				{
					Direction     = pad.direction,
					VelocityReset = pad.reset,
					Force         = pad.force,
					Position      = padTransform.Position,

					Shooter = padEntity,
					Victim  = movableEntity
				}
			});
		}

		protected override void OnUpdate()
		{
			var launchPadChunks = m_LaunchPadQuery.CreateArchetypeChunkArray(Allocator.TempJob);
			var launchPadChunksEnumerator = launchPadChunks.GetEnumerator();

			var movableChunks = m_MovableQuery.CreateArchetypeChunkArray(Allocator.TempJob);
			var movableChunksEnumerator = movableChunks.GetEnumerator();
			
			var entityType = GetArchetypeChunkEntityType();
			var ltwType = GetArchetypeChunkComponentType<LocalToWorld>(true);
			var launchPadType = GetArchetypeChunkComponentType<LaunchPad>(true);

			while (launchPadChunksEnumerator.MoveNext())
			{
				var launchPadChunk = launchPadChunksEnumerator.Current;

				var entityArray    = launchPadChunk.GetNativeArray(entityType);
				var ltwArray       = launchPadChunk.GetNativeArray(ltwType);
				var launchPadArray = launchPadChunk.GetNativeArray(launchPadType);
				for (var i = 0; i != launchPadChunk.Count; i++)
				{
					var entity       = entityArray[i];
					var localToWorld = ltwArray[i];
					var launchPad    = launchPadArray[i];

					movableChunksEnumerator.Reset();
					while (movableChunksEnumerator.MoveNext())
					{
						var movableChunk = movableChunksEnumerator.Current;

						var movableEntityArray = movableChunk.GetNativeArray(entityType);
						var movableLtwArray    = movableChunk.GetNativeArray(ltwType);
						for (var j = 0; j != movableChunk.Count; j++)
						{
							//OnBump();
						}
					}
				}
			}

			launchPadChunksEnumerator.Dispose();
			launchPadChunks.Dispose();
			
			movableChunksEnumerator.Dispose();
			movableChunks.Dispose();
		}
	}
}