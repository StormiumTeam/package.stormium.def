using UnityEngine;

namespace Graphics.Weapons.Rocket
{
	public class RocketProjectileExplosion : MonoBehaviour
	{
		public AsyncAssetPool<GameObject> ParentPool;
		public Animator Animator;
		
		public void DoReset()
		{
			Animator.enabled = false;
		}

		public void StartAnimation()
		{
			Animator.enabled = true;
			Animator.Play("RocketExplosion_Anim_Play");
		}

		public void ReturnToPool()
		{
			gameObject.SetActive(false);
			ParentPool?.Enqueue(gameObject);
		}
	}
}