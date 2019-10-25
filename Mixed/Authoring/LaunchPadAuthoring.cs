using System;
using Stormium.Default.Mixed;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
	[InternalBufferCapacity(16)]
	public struct LaunchPadCooldown : IBufferElementData, IEquatable<Entity>
	{
		public Entity Target;
		public UTick  RemoveAtTick;

		public bool Equals(Entity entity)
		{
			return Target == entity;
		}
	}
	
	public class LaunchPadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public Vector3 direction;
		public Vector3 worldMomentum;
		public Vector3 localMomentum;
		public float   force;

		[Header("Editor Only - Simulation")]
		public float time = 1.0f;

		public float drag = 0.05f;
		public Vector3 gravity = new Vector3(0, -15, 0);

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new LaunchPad
			{
				direction     = direction.normalized,
				worldMomentum = worldMomentum,
				localMomentum = localMomentum,
				force         = force
			});
			dstManager.AddBuffer<LaunchPadCooldown>(entity);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawRay(transform.position, direction.normalized);
			Gizmos.DrawRay(transform.up, Vector3.up * force);
			Gizmos.color = Color.green;
			
			var pos = transform.position;
			var vel = direction.normalized * force;
			var delta = 0.1f;
			var remainingTime = time;
			while (remainingTime > 0)
			{
				vel += gravity * delta;
				vel.x = Mathf.Lerp(vel.x, 0, delta * drag);
				vel.z = Mathf.Lerp(vel.z, 0, delta * drag);
				Gizmos.DrawRay(pos, vel * delta);
				pos += vel * delta;
				remainingTime -= delta;
			}
		}
	}
}