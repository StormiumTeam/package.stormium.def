using package.guerro.shared;
using UnityEngine;

namespace package.stormium.def.Utilities
{
    public static class UtilityWallRayTrace
    {
        private const float WallDistance        = 0.19f;
        private const float WallMinimalDistance = 0.5f;
        private const float WallMaximalDistance = 1.1f;

        /// <summary>
        /// Launch a ray with a capsule shape.
        /// </summary>
        /// <param name="dodgeDir">The dodge direction (ray direction)</param>
        /// <param name="startPosition">The start position (the center of the capsule)</param>
        /// <param name="radius">The radius of the capsule</param>
        /// <param name="skinWidth">The skin width of the capsule</param>
        /// <param name="height">The height of the capsule</param>
        /// <param name="substractHeight">The height to substract from the capsule</param>
        /// <returns></returns>
        public static RaycastHit RayTrace(ref Vector3 dodgeDir, ref Vector3 startPosition,
                                           ref float   radius,   ref float   skinWidth, ref float height,
                                           ref float   substractHeight)
        {
            // We normalize the dodge direction to dodge some problems
            dodgeDir.Normalize();

            // Get the final height, low point and high point of the capsule collider
            var finalHeight = height - substractHeight;
            var lowPoint    = startPosition - new Vector3(0, finalHeight * 0.5f, 0);
            var highPoint   = startPosition + new Vector3(0, finalHeight * 0.5f, 0);

            // Get the distance for the ray
            var rayDistance = Mathf.Clamp(radius * 0.5f + skinWidth + WallDistance, WallMinimalDistance,
                WallMaximalDistance);

            RaycastHit raycastHit;
            // Launch a ray from a capsule shape, if it hit, return it
            // Or else we will use a BSP method to calculate the nearest point.
            if (Physics.CapsuleCast(lowPoint, highPoint, radius - skinWidth, dodgeDir, out raycastHit, rayDistance))
                return raycastHit;

            // === === === === === === === === === === ===
            // --- BSP method to get the nearest point ---
            // === === === === === === === === === === ===
            
            // Default variables
            // Used to know the nearest points and normals
            var nearestPoint  = new Vector3();
            var nearestNormal = new Vector3();

            // The distance between the nearest point and the start position
            var distance = -1f;

            // We create the capsule radius for the overlap method
            var overlapRadius = Mathf.Clamp(radius + skinWidth + (WallDistance * 2), WallMinimalDistance * 2,
                WallMaximalDistance * 2);
            // We get the overlaps
            var overlaps = Physics.OverlapCapsule(lowPoint, highPoint, overlapRadius);
            // We iterate the overlaps
            for (int i = 0; i != overlaps.Length; i++)
            {
                var overlap      = overlaps[i];
                // We get the referencable gameobject for performance (referencable gameobjects cache components)
                var referencable = ReferencableGameObject.GetComponent<ReferencableGameObject>(overlap.gameObject);
                // We get the result from trying to get the BSPTree component
                var bspTreeGetResult = referencable.GetComponentFast<BSPTreeMeshCollision>();
                if (!bspTreeGetResult.HadIt) //< Verify it there is a BSPTree on this component
                    continue;

                // Get the BSPTree from the result
                var bspTree = bspTreeGetResult.Value;

                // Create the normal variable that will be used to compare the nearest normal
                Vector3 normal;
                // Get the closest point in the BSPTree
                var     point        = bspTree.ClosestPointOn(startPosition, overlapRadius * 1000, out normal);
                // Get the temporary distance (capsulated in a variable for performance reason)
                var     tempDistance = Vector3.Distance(point, nearestPoint);

                // Verify if we can overwrite the nearest point and normal by verifying the distance
                // (or if it's the first time we verify it)
                if (i == 0
                    || tempDistance < distance)
                {
                    // Set our new values
                    
                    // Set the new distance for the next comparison
                    distance      = tempDistance;
                    nearestPoint  = point;
                    nearestNormal = normal;
                }
            }

            // We found a point
            if (distance >= 0)
            {
                Debug.Log(nearestNormal);
                
                return new RaycastHit()
                {
                    point  = nearestPoint,
                    normal = nearestNormal,
                };
            }

            return raycastHit;
        }
    }
}