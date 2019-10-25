using System;
using System.Collections.Generic;
using CharacterController;
using package.stormiumteam.shared.ecs;
using ProKit;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Material = UnityEngine.Material;
using Object = UnityEngine.Object;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(CharCamera))]
	[UpdateBefore(typeof(UpdateCameraSystem))]
	[AlwaysUpdateSystem]
	public class CharacterSnapshotSystem : ComponentSystem
	{
		private EntityQuery m_EntityWithoutComponentQuery;
		private Material m_Material;
		private UnityEngine.Mesh m_Mesh;

		// that quite ugly...
		private static event Action onSystemUpdate;

		class SynchronizeTransform : MonoBehaviour
		{
			public Entity        entity;
			public EntityManager em;

			private void OnEnable()
			{
				onSystemUpdate += OnUpdate;
			}

			private void OnDisable()
			{
				onSystemUpdate -= OnUpdate;
			}

			private void OnUpdate()
			{
				transform.position = em.GetComponentData<Translation>(entity).Value;
			}
			
			public void SetTarget(EntityManager em, Entity entity)
			{
				this.em     = em;
				this.entity = entity;
			}
		}

		protected override void OnStartRunning()
		{
			base.OnCreate();

			m_EntityWithoutComponentQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(CharacterSnapshot)},
				None = new ComponentType[] {typeof(CharacterComponent)}
			});
			
			Debug.Log("1");
			m_Material = GameObject.Find("Capsule").GetComponent<MeshRenderer>().material;
			Debug.Log("2");
			m_Mesh = Object.Instantiate(GameObject.Find("Capsule").GetComponent<MeshFilter>().mesh);
			Debug.Log("3");
			
			var vertices = new List<Vector3>();
			m_Mesh.GetVertices(vertices);
			Debug.Log("4");
			for (var i = 0; i != vertices.Count; i++)
			{
				var vert = vertices[i];
				vert.y += 0.5f;
				vertices[i] = vert;
			}
			m_Mesh.SetVertices(vertices);
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
					EntityManager.SetOrAddComponentData(entity, new CurrentSimulatedPosition());
					EntityManager.SetOrAddComponentData(entity, new CurrentSimulatedRotation());
					EntityManager.SetOrAddComponentData(entity, new LocalToWorld());

					var go = new GameObject();
					go.AddComponent<MeshFilter>().mesh = m_Mesh;
					go.AddComponent<MeshRenderer>().material = m_Material;
					go.AddComponent<DestroyGameObjectOnEntityDestroyed>().SetTarget(EntityManager, entity);
					go.AddComponent<SynchronizeTransform>().SetTarget(EntityManager, entity);
				}
			}

			EntityManager.AddComponent(m_EntityWithoutComponentQuery, typeof(CharacterComponent));
		}
	}
}