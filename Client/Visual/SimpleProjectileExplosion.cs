using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

namespace Stormium.Default.Client.Visual
{
	public class SimpleProjectileExplosion : ProjectileExplosionPresentationBase
	{
		public float timeBeforePooling = 1.25f;
		public float PoolingProgress { get; set; }

		public string animatorTriggerEvent = "OnExplosionTrigger";

		public Animator[]     animatorArray;
		public VisualEffect[] vfxArray;

		public override void OnReset()
		{
			base.OnReset();

			PoolingProgress = 0.0f;
			Execute();
		}

		public void Execute()
		{
			if (animatorArray != null)
				foreach (var animator in animatorArray)
				{
					animator.enabled = true;
					animator.Play(animatorTriggerEvent);
				}

			if (vfxArray != null)
				foreach (var vfx in vfxArray)
				{
					vfx.enabled = true;
					vfx.initialEventName = "OnAnimEvent";
					vfx.Reinit();
				}
		}
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class SimpleProjectileExplosionSystem : ComponentSystem
	{		
		protected override void OnUpdate()
		{
			Entities.ForEach((ProjectileExplosionBackend backend) =>
			{
				if (backend.Presentation is SimpleProjectileExplosion p)
				{
					Execute(backend, p);
				}
			});
		}

		private void Execute(ProjectileExplosionBackend backend, SimpleProjectileExplosion presentation)
		{
			if (presentation.PoolingProgress > presentation.timeBeforePooling)
			{
				if (presentation.animatorArray != null)
					foreach (var animator in presentation.animatorArray)
						animator.enabled = false;

				if (presentation.vfxArray != null)
					foreach (var vfx in presentation.vfxArray)
						vfx.enabled = false;

				backend.Return(false, true);
				return;
			}

			presentation.PoolingProgress += Time.DeltaTime;
		}
	}
}