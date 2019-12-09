using Unity.NetCode;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition; 

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ClientInitializationSystemGroup))]
	public class SetAntialising : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((GameCamera gm) =>
			{
				var camera = gm.Camera;
				var additionalHdCameraData = camera.GetComponent<HDAdditionalCameraData>();
				if (additionalHdCameraData == null)
					additionalHdCameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
				
				if (additionalHdCameraData != null && additionalHdCameraData.antialiasing == HDAdditionalCameraData.AntialiasingMode.None)
				{
					var hdCamera = HDCamera.GetOrCreate(camera);
					var hdrpPipeline = (HDRenderPipeline) RenderPipelineManager.currentPipeline;
					additionalHdCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
					
					hdCamera.Update(hdCamera.frameSettings, hdrpPipeline, MSAASamples.None, hdCamera.xr ?? new XRPass());
				}
			});
		}
	}
}