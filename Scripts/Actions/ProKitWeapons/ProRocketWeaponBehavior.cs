using Unity.Entities;
using UnityEngine;

namespace Scripts.Actions.ProKitWeapons
{
	public class ProRocketWeaponBehavior : MonoBehaviour
	{
		public AudioSource FireSound;
	}

	public class ProRocketWeaponBehaviorSystemUpdate : ComponentSystem
	{
		protected override void OnUpdate()
		{
			ForEach((ProRocketWeaponBehavior weapon) =>
			{
				
			});
		}
	}
}