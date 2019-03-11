using System.Collections.Generic;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Stormium.Default.NexG
{
	public struct NexG_ChatBox_LineData
	{
		public GameObject TextLine;
		public Vector2 Bounds;
	}
	
	public class NexG_ChatBox_Container : MonoBehaviour
	{
		private GameObject[] m_CreatedLines;
		
		[SerializeField]
		internal List<string> inputs = new List<string>();
		
		public GameObject PrefabTextLine;
		public Transform CreateTextRoot;

		void OnDisable()
		{
			DestroyLines();
		}

		private void DestroyLines()
		{
			if (m_CreatedLines == null) return;

			foreach (var line in m_CreatedLines)
			{
				if (line) DestroyImmediate(line);
			}
		}
		
		public void UpdateFromInputs(List<string> inputs)
		{
			this.inputs = inputs;
			if (m_CreatedLines == null || inputs.Count != m_CreatedLines.Length)
			{
				DestroyLines();
				m_CreatedLines = new GameObject[inputs.Count];
			}

			var position = CreateTextRoot.transform.position;
			for (var i = inputs.Count; i-->0;)
			{
				var textLine = m_CreatedLines[i] ?? Instantiate(PrefabTextLine, Vector3.zero, quaternion.identity, transform);
				m_CreatedLines[i] = textLine;

				var nexGTextLine = textLine.GetComponent<NexG_ChatBox_TextLine>();
				nexGTextLine.Rebuild(inputs[i]);
				var firstLabel = nexGTextLine.Labels[0];
				
				textLine.transform.position = position + Vector3.up * firstLabel.preferredHeight * 0.5f;
				
				position.y += firstLabel.preferredHeight * 0.5f;
			}
		}
	}

	[ExecuteAlways, AlwaysUpdateSystem]
	public class NexG_ChatBoxSystem : GameBaseSystem
	{
		private ComponentGroup m_Group;
		
		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_Group = GetComponentGroup(typeof(NexG_ChatBox_Container));
		}

		protected override void OnUpdate()
		{
			var componentArray = m_Group.GetComponentArray<NexG_ChatBox_Container>();
			for (var i = 0; i != componentArray.Length; i++)
			{
				var container = componentArray[i];
				
				container.UpdateFromInputs(container.inputs);
			}
		}
	}
}