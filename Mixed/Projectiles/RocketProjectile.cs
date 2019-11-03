using package.stormiumteam.shared.ecs;
using Stormium.Core.Projectiles;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Projectiles
{
	public struct RocketProjectile : IComponentData
	{
		public float DetectionRadius;
		public float ExplosionRadius;
	}

	public unsafe class RocketProjectileSystem : JobGameBaseSystem
	{
		private struct UpdateJob : IJobForEach_CCC<RocketProjectile, Translation, Velocity>
		{
			public UTick Tick;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld;

			public void Execute(ref RocketProjectile rocket, ref Translation translation, ref Velocity velocity)
			{
				var blobCollider = SphereCollider.Create(new SphereGeometry {Radius = rocket.DetectionRadius});
				ref var collider = ref blobCollider.Value;

				var input = new ColliderCastInput
				{
					Collider = (Collider*) UnsafeUtility.AddressOf(ref collider)
				};
				var end = ProjectileUtility.Project(translation.Value, ref velocity.Value, Tick.Delta);
			}
		}

		private LazySystem<BuildPhysicsWorld> m_BuildPhysicsWorld;

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new UpdateJob
			{
				Tick         = GetTick(true),
				PhysicsWorld = this.L(ref m_BuildPhysicsWorld).PhysicsWorld
			}.Schedule(this, inputDeps);
		}
	}
}