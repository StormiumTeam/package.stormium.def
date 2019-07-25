using System.Collections.Generic;
using StormiumTeam.GameBase;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace GUIScripts.DebugWindow
{
	[RequireComponent(typeof(GameObjectEntity))]
	public class DebugWindowBehavior : MonoBehaviour
	{
		public TextMeshProUGUI LinesText;
	}

	public class DebugWindowBehaviorSystem : ComponentSystem
	{
		private const int MaxLines = 10;
		private List<string> m_ToLog = new List<string>();
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			GameDebug.onLogUpdate += OnLogUpdate;
		}

		private void OnLogUpdate(string str, LogType type)
		{
			string logStr;
			switch (type)
			{
				case LogType.Error:
					logStr = ($"<color=red>{str}</color>");
					break;
				case LogType.Warning:
					logStr = ($"<color=yellow>{str}</color>");
					break;
				default:
					logStr = ($"<color=white>{str}</color>");
					break;
			}

			m_ToLog.Add(logStr);
		}

		private void Append(DebugWindowBehavior debugWindow, string str)
		{
			var newText = str + "\n" + debugWindow.LinesText.text;
			
			debugWindow.LinesText.text = newText.Substring(0, math.min(newText.Length, 2000));
		}

		protected override void OnStartRunning()
		{
			Entities.ForEach((DebugWindowBehavior debugWindow) => { debugWindow.LinesText.text = ".\n.\n.\n.\n.\n.\n.\n.\n.\n."; });
		}

		protected override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.P))
				Debug.Log("Print");
			if (Input.GetKeyDown(KeyCode.W))
				Debug.LogWarning("Warning");
			if (Input.GetKeyDown(KeyCode.E))
				Debug.LogError("Error");
			
			Entities.ForEach((DebugWindowBehavior debugWindow) =>
			{
				foreach (var str in m_ToLog)
					Append(debugWindow, str);
			});
			m_ToLog.Clear();
		}
	}
}