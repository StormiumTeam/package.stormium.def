using System;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace package.stormium.def.Utilities
{
    public static class UtilityWallRayTrace
    {
        private const float WallDistance        = 0.21f;
        private const float WallMinimalDistance = 0.4f;
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
                                           ref float   substractHeight, Collider convexCollider)
        {
            // We normalize the dodge direction to dodge some problems
            dodgeDir.Normalize();

            // Get the final height, low point and high point of the capsule collider
            var finalHeight = height - substractHeight;
            var lowPoint    = startPosition - new Vector3(0, finalHeight * 0.5f, 0);
            var highPoint   = startPosition + new Vector3(0, finalHeight * 0.5f, 0);

            // Get the distance for the ray
            var rayDistance = Mathf.Clamp(radius * 0.5f + skinWidth + (WallDistance * 2), WallMinimalDistance * 2,
                WallMaximalDistance * 2);

            RaycastHit raycastHit = default;
            // Launch a ray from a capsule shape, if it hit, return it
            // Or else we will use a BSP method to calculate the nearest point.
            /*if (Physics.CapsuleCast(lowPoint, highPoint, radius - skinWidth, dodgeDir, out raycastHit, rayDistance))
                return raycastHit;*/

            // === === === === === === === === === === ===
            // --- BSP method to get the nearest point ---
            // === === === === === === === === === === ===
            
            // Default variables
            // Used to know the nearest points and normals
            var nearestPoint  = new Vector3();
            var nearestNormal = new Vector3();

            // The distance between the nearest point and the start position
            var distance = float.MaxValue;

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
                var meshGetResult = referencable.GetComponentFast<MeshCollider>();
                if (!meshGetResult.HasValue) //< Verify it there is a BSPTree on this component
                    continue;
                MeshCollider meshCollider = meshGetResult;

                using (var request = CPhysicTracer.Active.Get<CapsuleCollider>())
                {
                    var collider = request.Collider;
                    collider.radius = overlapRadius * 0.6f;
                    collider.height = finalHeight;
                    collider.transform.position = startPosition;
                    
                    if (Physics.ComputePenetration(collider, startPosition, Quaternion.identity,
                        meshCollider, meshCollider.transform.position, meshCollider.transform.rotation,
                        out nearestNormal, out distance))
                    {
                        Debug.Log("Computed: " + distance);
                        return new RaycastHit()
                        {
                            point  = nearestPoint,
                            normal = nearestNormal.ToGrid(1).normalized
                        };
                    }
                }

                // Get the BSPTree from the result
                /*var meshCollider = meshGetResult.Value;

                // Create the normal variable that will be used to compare the nearest normal
                Vector3 normal;
                // Get the closest point in the BSPTree
                var triangleResult = GetClosestPoint(meshCollider, startPosition);
                // Get the temporary distance (capsulated in a variable for performance reason)
                var     tempDistance = Vector3.Distance(triangleResult.closestPoint, nearestPoint);

                // Verify if we can overwrite the nearest point and normal by verifying the distance
                // (or if it's the first time we verify it)
                if (tempDistance < distance)
                {
                    // Set the new distance for the next comparison
                    distance      = tempDistance;
                    nearestPoint  = triangleResult.closestPoint;
                    nearestNormal = triangleResult.normal;
                }*/
            }

            // We found a point
            /*if (nearestNormal != Vector3.zero)
            {
                Debug.DrawRay(nearestPoint, nearestNormal, Color.green, 5f);
                return new RaycastHit()
                {
                    point  = nearestPoint,
                    normal = startPosition,
                };
            }*/

            return raycastHit;
        }

        public static TriangleResult GetClosestPoint(MeshCollider meshFilter, Vector3 position)
        {
            var mesh = meshFilter.sharedMesh;
            var result = new NativeArray<TriangleResult>(1, Allocator.TempJob);
            var resultDistance = new NativeArray<float>(1, Allocator.TempJob) {[0] = float.PositiveInfinity};
            var triangles = GetNativeVertexArrays(mesh.triangles);
            var vertices = GetNativeVertexArrays(mesh.vertices);

            new JobFindClosestPoint()
            {   
                Target = meshFilter.transform.InverseTransformPoint(position),
                Result = result,
                ResultDistance = resultDistance,
                Triangles = triangles,
                Vertices = vertices,
                Length = triangles.Length / 3
            }.Run(triangles.Length / 3);

            var finalResult = result[0];
            finalResult.closestPoint = meshFilter.transform.TransformPoint(finalResult.closestPoint);
            finalResult.normal = (finalResult.closestPoint.ToGrid(1) - position.ToGrid(1)).normalized;
            
            result.Dispose();
            resultDistance.Dispose();
            triangles.Dispose();
            vertices.Dispose();
            
            return finalResult;
        }
        
        static unsafe NativeArray<int> GetNativeVertexArrays(int[] vertexArray)
        {
            var verts = new NativeArray<int>(vertexArray.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            fixed (void* vertexBufferPointer = vertexArray)
            {
                // ...and use memcpy to copy the Vector3[] into a NativeArray<floar3> without casting. whould be fast!
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(verts),
                    vertexBufferPointer, vertexArray.Length * (long) UnsafeUtility.SizeOf<int>());
            }
            return verts;
        }
        
        static unsafe NativeArray<Vector3> GetNativeVertexArrays(Vector3[] vertexArray)
        {
            var verts = new NativeArray<Vector3>(vertexArray.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            fixed (void* vertexBufferPointer = vertexArray)
            {
                // ...and use memcpy to copy the Vector3[] into a NativeArray<floar3> without casting. whould be fast!
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(verts),
                    vertexBufferPointer, vertexArray.Length * (long) UnsafeUtility.SizeOf<Vector3>());
            }
            return verts;
        }

        public struct TriangleResult
        {
            public Vector3 closestPoint;
            public Vector3 normal;
            public Vector3 centre;
            public int triangle;
            public float   distanceSquared;
        }

        public struct JobFindClosestPoint : IJobParallelFor
        {
            public                                       Vector3                     Target;
            public                                       NativeArray<TriangleResult> Result;
            [NativeDisableParallelForRestriction] public NativeArray<float>          ResultDistance;
            [NativeDisableParallelForRestriction] public NativeArray<Vector3>        Vertices;
            [NativeDisableParallelForRestriction] public NativeArray<int>            Triangles;

            public int Length;

            public TriangleResult GetTriangleInfo(Vector3 point, int triangle)
            {
                var result = new TriangleResult();

                result.triangle        = triangle;
                result.distanceSquared = float.PositiveInfinity;

                if (triangle >= Length)
                    return result;


                //Get the vertices of the triangle
                var p1 = Vertices[Triangles[0 + triangle * 3]];
                var p2 = Vertices[Triangles[1 + triangle * 3]];
                var p3 = Vertices[Triangles[2 + triangle * 3]];

                result.normal = Vector3.Cross((p2 - p1).normalized, (p3 - p1).normalized);

                //Project our point onto the plane
                var projected = point + Vector3.Dot((p1 - point), result.normal) * result.normal;

                //Calculate the barycentric coordinates
                var u = ((projected.x * p2.y) - (projected.x * p3.y) - (p2.x * projected.y) + (p2.x * p3.y) + (p3.x * projected.y) - (p3.x * p2.y)) /
                        ((p1.x * p2.y) - (p1.x * p3.y) - (p2.x * p1.y) + (p2.x * p3.y) + (p3.x * p1.y) - (p3.x * p2.y));
                var v = ((p1.x * projected.y) - (p1.x * p3.y) - (projected.x * p1.y) + (projected.x * p3.y) + (p3.x * p1.y) - (p3.x * projected.y)) /
                        ((p1.x * p2.y) - (p1.x * p3.y) - (p2.x * p1.y) + (p2.x * p3.y) + (p3.x * p1.y) - (p3.x * p2.y));
                var w = ((p1.x * p2.y) - (p1.x * projected.y) - (p2.x * p1.y) + (p2.x * projected.y) + (projected.x * p1.y) - (projected.x * p2.y)) /
                        ((p1.x * p2.y) - (p1.x * p3.y) - (p2.x * p1.y) + (p2.x * p3.y) + (p3.x * p1.y) - (p3.x * p2.y));

                result.centre = p1 * 0.3333f + p2 * 0.3333f + p3 * 0.3333f;

                //Find the nearest point
                var vector = (new Vector3(u, v, w)).normalized;

                //work out where that point is
                var nearest = p1 * vector.x + p2 * vector.y + p3 * vector.z;
                result.closestPoint    = nearest;
                result.distanceSquared = (nearest - point).sqrMagnitude;

                if (float.IsNaN(result.distanceSquared))
                {
                    result.distanceSquared = float.PositiveInfinity;
                }

                return result;
            }

            public void Execute(int index)
            {
                var r = GetTriangleInfo(Target, index);
                if (ResultDistance[0] > r.distanceSquared)
                {
                    ResultDistance[0] = r.distanceSquared;
                    Result[0] = r;
                }
            }
        }
    }
}