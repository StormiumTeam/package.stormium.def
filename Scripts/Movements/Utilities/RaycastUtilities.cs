using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace package.stormium.def.Utilities
{
    public static class RaycastUtilities
    {
        private static NativeArray<Vector3> s_RepairNormalResult;

        static RaycastUtilities()
        {
            s_RepairNormalResult = new NativeArray<Vector3>(1, Allocator.Persistent);

            Application.quitting += () => s_RepairNormalResult.Dispose();
        }

        public static Vector3 SlideVelocityNoYChange(Vector3 velocity, Vector3 onNormal)
        {
            var oldY = velocity.y;

            velocity.y = 0;
            onNormal.y = 0;

            var desiredMotion = SlideVelocity(velocity, onNormal);
            desiredMotion.y = oldY;

            return desiredMotion;
        }
        
        public static Vector3 SlideVelocity(Vector3 velocity, Vector3 onNormal)
        {            
            var undesiredMotion = onNormal * Vector3.Dot(velocity, onNormal);
            var desiredMotion   = velocity - undesiredMotion;

            return desiredMotion;
        }
        
        public static Vector3 RepairHitSurfaceNormal(RaycastHit hit)
        {
            var collider = hit.collider;

            var meshCollider = collider as MeshCollider;
            if (meshCollider != null)
            {
                var mesh      = meshCollider.sharedMesh;
                var triangles = PhysicMeshTool.GetTriangles(mesh);
                var vertices  = PhysicMeshTool.GetVertices(mesh);
                
                new JobRepairHitSurfaceNormal()
                {
                    TriangleIndex = hit.triangleIndex,
                    Triangles     = triangles,
                    Vertices      = vertices,
                    ResultNormal  = s_RepairNormalResult
                }.Run();
                
                return hit.transform.TransformDirection(s_RepairNormalResult[0]);
            }

            return hit.normal;
        }

        [BurstCompile]
        private struct JobRepairHitSurfaceNormal : IJob
        {
            public int                  TriangleIndex;
            public NativeArray<int>     Triangles;
            public NativeArray<Vector3> Vertices;

            public NativeArray<Vector3> ResultNormal;
            
            public void Execute()
            {
                var v0 = Vertices[Triangles[TriangleIndex * 3]];
                var v1 = Vertices[Triangles[TriangleIndex * 3 + 1]];
                var v2 = Vertices[Triangles[TriangleIndex * 3 + 2]];

                ResultNormal[0] = Vector3.Cross(v1 - v0, v2 - v1).normalized;
            }
        }
    }
}