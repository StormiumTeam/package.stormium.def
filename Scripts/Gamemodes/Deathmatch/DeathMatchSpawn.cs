using UnityEngine;

namespace Stormium.Default.GameModes
{
	public class DeathMatchSpawn : MonoBehaviour
	{
		private void OnDrawGizmos()
		{
			var position = transform.position;
			
			Gizmos.DrawWireSphere(position, 0.1f);
			Gizmos.DrawRay(position, transform.forward);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.blue;
			
			var position = transform.position;
			
			Gizmos.DrawWireSphere(position, 0.1f);
			Gizmos.DrawRay(position, transform.forward);
		}
	}
}