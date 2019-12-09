using System;
using Authoring;
using package.stormiumteam.shared.ecs;
using Unity.NetCode;
using Stormium.Default.Mixed;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public unsafe class LaunchPadSimulation : JobGameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<LocalToWorld, LaunchPad, PhysicsCollider>
		{
			[ReadOnly] public UTick Tick;

			[ReadOnly] public NativeArray<ArchetypeChunk>                  MovableChunks;
			[ReadOnly] public ArchetypeChunkEntityType                     EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<LocalToWorld>    LocalToWorldType;
			[ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

			public BufferFromEntity<LaunchPadCooldown> CooldownFromEntity;

			[WriteOnly] public NativeList<TargetImpulseEvent> ImpulseEventList;

			private bool Bump(Entity padEntity,     LocalToWorld padTransform,     LaunchPad       pad, PhysicsCollider padCollider,
			                  Entity movableEntity, LocalToWorld movableTransform, PhysicsCollider movableCollider)
			{
				var padRigidTransform     = new RigidTransform(padTransform.Value);
				var movableRigidTransform = new RigidTransform(movableTransform.Value);

				if (!padCollider.ColliderPtr->CalculateAabb(padRigidTransform).Overlaps(movableCollider.ColliderPtr->CalculateAabb(movableRigidTransform)))
					return false;

				var cc = new CustomCollide(movableCollider, movableTransform);
				var collection = new CustomCollideCollection(ref cc);
				var penetrateInput = new ColliderDistanceInput
				{
					Collider    = padCollider.ColliderPtr,
					MaxDistance = 0f,
					Transform   = padRigidTransform
				};

				var anyCollector = new AnyHitCollector<DistanceHit>(0.0f);
				if (!collection.CalculateDistance(penetrateInput, ref anyCollector))
					return false;

				ImpulseEventList.Add(new TargetImpulseEvent
				{
					Origin      = padEntity,
					Destination = movableEntity,

					Force    = math.normalizesafe(pad.direction) * pad.force,
					Momentum = pad.worldMomentum,
					Position = padTransform.Position,
				});

				return true;
			}

			public void Execute(Entity padEntity, int index, [ReadOnly] ref LocalToWorld padTransform, [ReadOnly] ref LaunchPad launchPad, [ReadOnly] ref PhysicsCollider padCollider)
			{
				var cooldownBuffer = CooldownFromEntity[padEntity];
				// Delete previous cooldown...
				for (var c = 0; c != cooldownBuffer.Length; c++)
				{
					if (cooldownBuffer[c].RemoveAtTick >= Tick)
						continue;

					cooldownBuffer.RemoveAt(c);
					c--;
				}

				// We can't add more cooldown elements...
				if (cooldownBuffer.Length >= cooldownBuffer.Capacity)
					return;

				for (var chk = 0; chk != MovableChunks.Length; chk++)
				{
					var movableEntityArray    = MovableChunks[chk].GetNativeArray(EntityType);
					var movableTransformArray = MovableChunks[chk].GetNativeArray(LocalToWorldType);
					var movableColliderArray  = MovableChunks[chk].GetNativeArray(PhysicsColliderType);

					var count = MovableChunks[chk].Count;
					for (var ent = 0; ent != count; ent++)
					{
						if (cooldownBuffer.AsNativeArray().Contains(movableEntityArray[ent]))
							continue;

						if (Bump
						(
							padEntity, padTransform, launchPad, padCollider,
							movableEntityArray[ent], movableTransformArray[ent], movableColliderArray[ent]
						))
						{
							var tick = UTick.AddMsNextFrame(Tick, 250);
							tick.Value++;
							cooldownBuffer.Add(new LaunchPadCooldown {Target = movableEntityArray[ent], RemoveAtTick = tick});
						}
					}
				}
			}
		}

		private EntityQuery m_LaunchPadQuery;
		private EntityQuery m_MovableQuery;

		private LazySystem<TargetImpulseEvent.Provider> m_TargetImpulseEventProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_LaunchPadQuery = GetEntityQuery(typeof(LocalToWorld), typeof(LaunchPad), typeof(PhysicsCollider));
			m_MovableQuery   = GetEntityQuery(typeof(LocalToWorld), typeof(MovableDescription), typeof(PhysicsCollider));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_MovableQuery.AddDependency(inputDeps);

			var movableChunks = m_MovableQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var dependency);
			inputDeps = new Job
			{
				Tick = GetTick(true),

				MovableChunks       = movableChunks,
				EntityType          = GetArchetypeChunkEntityType(),
				LocalToWorldType    = GetArchetypeChunkComponentType<LocalToWorld>(true),
				PhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>(true),

				ImpulseEventList = this.L(ref m_TargetImpulseEventProvider).GetEntityDelayedList(),

				CooldownFromEntity = GetBufferFromEntity<LaunchPadCooldown>()
			}.ScheduleSingle(m_LaunchPadQuery, JobHandle.CombineDependencies(inputDeps, dependency));

			m_TargetImpulseEventProvider.Get(World);
			this.L(ref m_TargetImpulseEventProvider).AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}