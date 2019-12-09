using System;
using CharacterController;
using package.stormium.def;
using package.stormiumteam.shared.ecs;
using ProKit;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.NetCode;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(CharCamera))]
	[UpdateBefore(typeof(UpdateCameraSystem))]
	[AlwaysUpdateSystem]
	public class CharacterSnapshotSystem : ComponentSystem
	{
		private EntityQuery m_EntityWithoutComponentQuery;

		// that quite ugly...
		private static event Action onSystemUpdate;

		protected override void OnStartRunning()
		{
			base.OnCreate();

			m_EntityWithoutComponentQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(CharacterSnapshot)},
				None = new ComponentType[] {typeof(CharacterComponent)}
			});
		}

		protected override void OnUpdate()
		{
			onSystemUpdate?.Invoke();
			
			if (m_EntityWithoutComponentQuery.IsEmptyIgnoreFilter)
				return;
			
			using (var entities = m_EntityWithoutComponentQuery.ToEntityArray(Allocator.TempJob))
			{
				foreach (var entity in entities)
				{
					if (!EntityManager.HasComponent<PhysicsCollider>(entity))
						EntityManager.AddComponentData(entity, new PhysicsCollider
						{
							Value = CapsuleCollider.Create(new CapsuleGeometry
							{
								Radius  = 0.5f,
								Vertex0 = 0,
								Vertex1 = new float3(0, 1, 0),
							}, new CollisionFilter
							{
								BelongsTo    = 0b00000000000000010000000000000000,
								CollidesWith = 0b11111111111111101111111111111111
							})
						});
					if (!EntityManager.HasComponent<PhysicsCharacter>(entity))
						EntityManager.AddComponentData(entity, new PhysicsCharacter
						{
							MaxStepHeight = 0.25f
						});
					EntityManager.AddComponentData(entity, new CameraModifierData {FieldOfView = 90, Position = math.up() * 2, Rotation = quaternion.identity});
					EntityManager.AddComponent(entity, typeof(CharacterPass));
					EntityManager.AddComponentData(entity, new StandardGroundMovement {Settings = SrtGroundSettings.NewBase()});
					EntityManager.SetOrAddComponentData(entity, new LocalToWorld());
					EntityManager.SetOrAddComponentData(entity, default(Velocity));
				}
			}

			EntityManager.AddComponent(m_EntityWithoutComponentQuery, typeof(CharacterComponent));
		}
	}
}