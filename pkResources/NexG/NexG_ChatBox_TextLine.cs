using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Default.NexG
{
	[RequireComponent(typeof(GameObjectEntity))]
	public class NexG_ChatBox_TextLine : MonoBehaviour
	{
		private static List<TMP_SubMeshUI> m_SubMeshNoGc = new List<TMP_SubMeshUI>();

		internal bool IsDirty;

		public TextMeshProUGUI[] Labels;

		public void Rebuild(string text)
		{
			for (var i = 0; i != Labels.Length; i++)
			{
				var label = Labels[i];

				label.text = text.Replace(";", "\n");
				if (i == 0)
					continue;

				label.GetComponentsInChildren(m_SubMeshNoGc);
				foreach (var subMesh in m_SubMeshNoGc)
				{
					subMesh.gameObject.SetActive(false);
				}
			}
		}
	}
}