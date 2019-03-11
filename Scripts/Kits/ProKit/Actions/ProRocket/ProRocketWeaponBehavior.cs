using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Scripts.Actions.ProKitWeapons
{
	public class ProRocketWeaponBehavior : MonoBehaviour
	{
		public AudioClip FireSound;
		public ParticleSystem Explosion;
		public Animator Animator;

		public bool HasExploded;
	}

	public class ProRocketWeaponBehaviorSystemUpdate : ComponentSystem
	{
		protected override void OnUpdate()
		{
			ForEach((ProRocketWeaponBehavior weapon, ref ModelParent modelParent) =>
			{
				var parent = modelParent.Parent;
				var data = EntityManager.GetComponentData<ProProjectileData>(parent);

				if (data.Phase == StandardProjectilePhase.Exploded && !weapon.HasExploded)
				{
					weapon.Animator.Play("Explosion");
					weapon.HasExploded = true;
					weapon.Explosion.Play();
				}
			});
		}
	}
}