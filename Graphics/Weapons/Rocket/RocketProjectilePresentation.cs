using Stormium.Default;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace Graphics.Weapons.Rocket
{
	public class RocketProjectilePresentation : RuntimeAssetPresentation<RocketProjectilePresentation>
	{
		public VisualEffect   trailVisualEffect;
		public AudioClip      fireSound;
		public ParticleSystem explosion;
		public Animator       animator;
	}

	public class RocketProjectileBackend : RuntimeAssetBackend<RocketProjectilePresentation>
	{

		public float                   startTime;
		public float3                  offset;
		public bool                    hasExploded;
		public bool                    hasBeenAwake;
		public StandardProjectilePhase previousPhase = StandardProjectilePhase.None;
		public bool                    stopEffectNextFrame;

		public override void OnReset()
		{
			startTime           = 0;
			offset              = float3.zero;
			hasExploded         = false;
			hasBeenAwake        = false;
			previousPhase       = StandardProjectilePhase.None;
			stopEffectNextFrame = false;
		}
	}

	public class VisualRocketProjectileSystem : VisualProjectileSystemBase<ProRocketProjectile, RocketProjectilePresentation, RocketProjectileBackend>
	{
		public AsyncAssetPool<GameObject> ExplosionPool;

		protected override string PresentationAssetId => "Stormium.Default.ProKit.Projectile.Rocket";

		protected override void SetPools()
		{
			base.SetPools();

			ExplosionPool = new AsyncAssetPool<GameObject>("Stormium.Default.ProKit.Projectile.Rocket.Explosion");
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			Entities.With(QueryBackend).ForEach((Transform tr, RocketProjectileBackend backend) =>
			{
				if (CheckAndDisableForNextFrame(backend)) return;

				var presentation = backend.Presentation;

				var projectileState = EntityManager.GetComponentData<ProProjectile.PredictedState>(backend.DstEntity);
				var localToWorld    = EntityManager.GetComponentData<LocalToWorld>(backend.DstEntity);
				var velocity        = EntityManager.GetComponentData<Velocity>(backend.DstEntity);

				var cameraTr = Camera.main.transform;

				if (backend.startTime <= 0.0001f)
				{
					backend.startTime = Time.time;
					backend.offset    = (cameraTr.right.normalized * 0.33f) + -(cameraTr.up.normalized * 0.15f) + (cameraTr.forward.normalized * 0.1f);
				}

				tr.forward = velocity.normalized;

				if (presentation != null && projectileState.phase != backend.previousPhase)
				{
					if (projectileState.phase == StandardProjectilePhase.Active)
					{
						presentation.trailVisualEffect.Reinit();
						presentation.trailVisualEffect.Play();

						presentation.trailVisualEffect.SetBool("stop", false);

						backend.stopEffectNextFrame = false;
					}
					else
					{
						presentation.trailVisualEffect.SetBool("stop", true);
						presentation.trailVisualEffect.Simulate(Time.deltaTime);

						backend.stopEffectNextFrame = true;
					}

					if (projectileState.phase == StandardProjectilePhase.Ended)
					{
						ExplosionPool.Dequeue((op) =>
						{
							op.SetActive(true);
							op.transform.up       = projectileState.explodeNormalHit;
							op.transform.position = localToWorld.Position;

							op.GetComponent<RocketProjectileExplosion>().ParentPool = ExplosionPool;
							op.GetComponent<RocketProjectileExplosion>().DoReset();
							op.GetComponent<RocketProjectileExplosion>().StartAnimation();
						});
					}

					presentation.trailVisualEffect.SetFloat("time", Time.time - backend.startTime);

					backend.previousPhase = projectileState.phase;
				}
				else
				{
					if (backend.stopEffectNextFrame)
						presentation.trailVisualEffect.Stop();
				}

				if (projectileState.phase == StandardProjectilePhase.Active)
				{
					var lpProgress = math.clamp(math.abs(Time.time - backend.startTime) * 0.9f, 0, 1);

					backend.offset = math.lerp(backend.offset, 0, lpProgress);
					tr.position    = localToWorld.Position + backend.offset;
				}
			});
		}
	}
}