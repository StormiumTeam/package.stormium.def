using System.Collections.Generic;
using Noesis;
using Unity.NetCode;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Stormium.Default.Client.Visual.Interfaces
{
	public struct CreateHudData
	{
		public string     Name;
		public NoesisXaml Xaml;

		public bool ActiveOnCreation;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class HudManager : ComponentSystem
	{
		private List<HudElement> m_Elements;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Elements = new List<HudElement>(32);
		}

		protected override void OnUpdate()
		{
			foreach (var element in m_Elements)
			{
				element.View.gameObject.GetComponent<HDAdditionalCameraData>().fullscreenPassthrough = true;
				
				if (element.Active != element.FlagActive)
				{
					element.Active = element.FlagActive;
					element.View.gameObject.SetActive(element.Active);
				}
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			foreach (var element in m_Elements)
			{
				element.Destroy();
			}
		}

		public HudElement CreateHud(CreateHudData createCtx)
		{
			var element = new HudElement();
			{
				var gameObject = new GameObject($"{createCtx.Name} Interface(World={World})");
				gameObject.SetActive(false);
				{
					var camera = gameObject.AddComponent<Camera>();
					camera.clearFlags  = CameraClearFlags.Nothing;
					camera.cullingMask = 0;

					var hdAdditionalCameraData = gameObject.AddComponent<HDAdditionalCameraData>();
					hdAdditionalCameraData.fullscreenPassthrough = true;
					hdAdditionalCameraData.volumeLayerMask = 0;

					var noesisView = gameObject.AddComponent<NoesisView>();
					noesisView.Xaml                = createCtx.Xaml;
					noesisView.ContinuousRendering = true;
					noesisView.IsPPAAEnabled       = true;

					element.View = noesisView;
				}
				gameObject.SetActive(createCtx.ActiveOnCreation);
				
				element.Active     = createCtx.ActiveOnCreation;
				element.FlagActive = element.Active;
			}
			m_Elements.Add(element);
			return element;
		}
	}

	public class HudElement
	{
		internal bool FlagActive;
		public bool Active { get; set; }

		public NoesisView       View;
		public FrameworkElement Content => View.Content;

		public void Destroy()
		{
			Debug.LogError("Why do you call that");

			Object.Destroy(View.gameObject);
		}
	}
}