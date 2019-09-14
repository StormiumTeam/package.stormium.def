using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.States
{
    /// <summary>
    /// This tag shouldn't be attached to the camera directly.
    /// Create an entity with this component and Position and Rotation.
    /// Then set this new entity to CameraState.Target.
    /// </summary>
    public struct LocalCameraFreeMove : IComponentData
    {
        public float2 PreviousMove;
        public float  PreviousJet;
        public float2 PreviousAimLook;
        public float  Intensity;
    }
}