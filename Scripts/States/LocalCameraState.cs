using Stormium.Core;
using StormiumShared.Core.Networking;
using Unity.Entities;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace Stormium.Default.States
{
    public struct LocalCameraState : IStateData, IComponentData
    {
        public CameraMode Mode;

        public Entity Target;
    }

    /// <summary>
    /// This tag shouldn't be attached to the camera directly.
    /// Create an entity with this component and Position and Rotation.
    /// Then set this new entity to CameraState.Target.
    /// </summary>
    public struct LocalCameraFreeMove : IComponentData
    {
        public float Intensity;
    }
}