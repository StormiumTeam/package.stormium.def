using System;
using System.Threading;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Stormium.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace Stormium.Default.Tests
{
    /*[UpdateBefore(typeof(UpdateLoop.BeforeDataChange))]
    public class TestSnapshotManagerUpdatePosition : ComponentSystem
    {
        private ComponentGroup m_ComponentGroup;

        protected override void OnCreateManager()
        {
            m_ComponentGroup = GetComponentGroup(typeof(GenerateEntitySnapshot), typeof(SnapshotEntityDataTransformSystem.State));
        }
        
        protected override void OnStartRunning()
        {
            var k = GameObject.CreatePrimitive(PrimitiveType.Cube);
            k.SetActive(false);
            meshQuad   = k.GetComponent<MeshFilter>().mesh;
            m_Material = k.GetComponent<MeshRenderer>().material;
        }
        
        private Mesh     meshQuad;
        private Material m_Material;

        protected override void OnUpdate()
        {
            var length      = m_ComponentGroup.CalculateLength();
            var states      = m_ComponentGroup.GetComponentDataArray<SnapshotEntityDataTransformSystem.State>();
            var entityArray = m_ComponentGroup.GetEntityArray();

            for (var i = 0; i != length; i++)
            {
                var entity = entityArray[i];

                if (Time.frameCount < 5 || Input.GetKey(KeyCode.I))
                {
                    if (Time.frameCount < 5 || Time.time % i / i > 0.25f)
                    {
                        states[i] = new SnapshotEntityDataTransformSystem.State
                        {
                            Position = new float3(Mathf.PingPong(Time.time + i * 0.01f, i * 0.1f))
                        };
                    }
                }

                var matrix1 = Matrix4x4.TRS((float3) states[i].Position, Quaternion.identity, Vector3.one * 0.25f);
                Graphics.DrawMesh(meshQuad, matrix1, m_Material, 0);
            }
        }
    }

    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(UpdateLoop.AfterDataChange))]
    public class TestSnapshotManager : ComponentSystem, INativeEventOnGUI
    {
        public unsafe struct TestGen
        {
            public bool                IsIncremental;
            public NativeArray<IntPtr> Results;
            public int Size;

            public void SetFullResult(SnapshotManager.GenerateResult result)
            {
                Dispose();
                
                IsIncremental = false;

                var arg1 = UnsafeUtility.SizeOf<SnapshotManager.GenerateResult>();
                var arg2 = UnsafeUtility.AlignOf<SnapshotManager.GenerateResult>();
                var arg3 = Allocator.Persistent;
                var ptr  = UnsafeUtility.Malloc(arg1, arg2, arg3);
                
                UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref result), arg1);
                
                Results = new NativeArray<IntPtr>(1, Allocator.Persistent) {[0] = (IntPtr) ptr};
                Size = Results.Length;
            }

            public void SetResultSize(int size)
            {
                Dispose();

                IsIncremental = true;
                Results = new NativeArray<IntPtr>(size, Allocator.Persistent);
                Size = 0;
            }

            public void AddResult(SnapshotManager.GenerateResult result, int index)
            {
                Size += result.Data.Length;
                
                var arg1 = UnsafeUtility.SizeOf<SnapshotManager.GenerateResult>();
                var arg2 = UnsafeUtility.AlignOf<SnapshotManager.GenerateResult>();
                var arg3 = Allocator.Persistent;
                var ptr = UnsafeUtility.Malloc(arg1, arg2, arg3);
                
                UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref result), arg1);
                
                Results[index] = (IntPtr) ptr;
            }

            public void Dispose()
            {
                if (Results.IsCreated)
                {
                    for (var i = 0; i != Results.Length; i++)
                    {
                        if (Results[i] != IntPtr.Zero)
                            UnsafeUtility.Free(Results[i].ToPointer(), Allocator.Persistent);
                    }
                }
                
                Results.Dispose();
            }
        }

        private ComponentGroup m_ComponentGroup;

        protected override void OnCreateManager()
        {
            m_ComponentGroup = GetComponentGroup(typeof(GenerateEntitySnapshot), typeof(SnapshotEntityDataTransformSystem.State));

            for (var i = 0; i != 64; i++)
            {
                var e = EntityManager.CreateEntity
                (
                    typeof(SnapshotEntityDataTransformSystem.State),
                    typeof(DataChanged<SnapshotEntityDataTransformSystem.State>),
                    typeof(GenerateEntitySnapshot)
                );
                EntityManager.SetComponentData(e, new SnapshotEntityDataTransformSystem.State
                {
                    Position = new float3(i * 2, i * 4, i * 8)
                });
            }
        }

        protected override void OnStartRunning()
        {
            // Create time entity
            var e = EntityManager.CreateEntity(typeof(GameTimeComponent), typeof(SimulateEntity));
            Debug.Log(e);
            World.GetExistingManager<StGameTimeManager>().SetSingleton(e);
            
            // Create client entity
            EntityManager.CreateEntity(typeof(ClientTag), typeof(StormiumClient), typeof(StormiumLocalTag));
            
            World.GetOrCreateManager<AppEventSystem>()
                 .SubscribeTo<INativeEventOnGUI, INativeEventOnGUI>(this);

            m_Gen = new TestGen
            {
                Results = new NativeArray<IntPtr>(0, Allocator.Persistent)
            };
        }

        private TestGen m_Gen;
        private int m_IncrementalCounter;
        private bool m_IsReadingIncremental;

        protected override unsafe void OnUpdate()
        {
            var snapshotMgr = World.GetExistingManager<SnapshotManager>();
            var gt = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
            var localBank = World.GetExistingManager<NetPatternSystem>().GetLocalBank();
            
            // Take a snapshot
            if (Input.GetKeyDown(KeyCode.T))
            {
                m_IncrementalCounter = 0;
                
                var result = snapshotMgr.GenerateLocalSnapshot(Allocator.Persistent, SnapshotFlags.FullDataAndLocal, default); 
                if (!result.IsCreated)
                {
                    Debug.LogWarning("Couldn't generate at frame #" + Time.frameCount);
                }
                else
                {
                    Debug.Log($"Snapshot taken at frame #{Time.frameCount} Tick: {{{result.Runtime.Header.GameTime.Tick}}}. Size: {result.Data.Length}B");
                }
                
                m_Gen.SetFullResult(result);
            }
            // Read a snapshot
            else if (Input.GetKey(KeyCode.R))
            {
                if (m_Gen.Results.Length == 0)
                {
                    Debug.LogWarning("No snapshot taken");
                    return;
                }

                UnsafeUtility.CopyPtrToStructure((void*) m_Gen.Results[0], out SnapshotManager.GenerateResult firstResult);
                Debug.Log
                (
                    m_Gen.IsIncremental
                        ? $"Reading incremental snapshot (Taken at: {{{firstResult.Runtime.Header.GameTime.Tick}}}, Reading Tick: {{{gt.Tick}}})"
                        : $"Reading full snapshot (Taken at: {{{firstResult.Runtime.Header.GameTime.Tick}}}, Reading Tick: {{{gt.Tick}}})"
                );

                var sender = new SnapshotSender();
                if ((m_Gen.IsIncremental && !m_IsReadingIncremental) || !m_Gen.IsIncremental)
                {
                    Debug.Log(m_IncrementalCounter);
                    
                    snapshotMgr.ApplySnapshotFromData(sender, new DataBufferReader(firstResult.Data), firstResult.Runtime, localBank);
                    if (m_Gen.IsIncremental)
                    {
                        m_IsReadingIncremental = true;
                        m_IncrementalCounter = 1;
                    }
                    else
                    {
                        m_IsReadingIncremental = false;
                        m_IncrementalCounter = 0;
                    }
                }
                else
                {
                    Debug.Log("Reading #" + m_IncrementalCounter);
                    
                    UnsafeUtility.CopyPtrToStructure((void*) m_Gen.Results[m_IncrementalCounter - 1], out SnapshotManager.GenerateResult prev);
                    UnsafeUtility.CopyPtrToStructure((void*) m_Gen.Results[m_IncrementalCounter], out SnapshotManager.GenerateResult curr);

                    snapshotMgr.ApplySnapshotFromData(sender, new DataBufferReader(curr.Data), prev.Runtime, localBank);
                    
                    m_IncrementalCounter++;
                    
                    Debug.Log(m_IncrementalCounter);

                    if (m_IncrementalCounter >= 127)
                    {
                        m_IsReadingIncremental = false;
                        m_IncrementalCounter = 0;
                        
                        Debug.Log("Finished reading.");
                        return;
                    }
                }
            }
            // Destroy
            else if (Input.GetKeyDown(KeyCode.D))
            {
                var entities = EntityManager.GetAllEntities(Allocator.Temp);
                foreach (var e in entities)
                {
                    if (EntityManager.HasComponent<GenerateEntitySnapshot>(e))
                        EntityManager.DestroyEntity(e);
                }
                entities.Dispose();
            }
            // Incremental snapshot (delta-instant snapshot)
            else if (Input.GetKey(KeyCode.I) && m_IncrementalCounter < 128)
            {
                SnapshotManager.GenerateResult result;

                // Take a full snapshot first
                if (m_IncrementalCounter == 0)
                {
                    m_Gen.SetResultSize(128);

                    result = snapshotMgr.GenerateLocalSnapshot(Allocator.Persistent, SnapshotFlags.FullDataAndLocal, default);
                    if (!result.IsCreated) Debug.LogWarning("Couldn't generate");
                    else Debug.Log("Incremental Start.");

                    m_Gen.AddResult(result, 0);

                    m_IncrementalCounter++;
                }
                else
                {
                    UnsafeUtility.CopyPtrToStructure((void*) m_Gen.Results[m_IncrementalCounter - 1], out SnapshotManager.GenerateResult prevResult);
                    
                    result = snapshotMgr.GenerateLocalSnapshot(Allocator.Persistent, SnapshotFlags.Local, prevResult);
                    if (!result.IsCreated) Debug.LogWarning("Couldn't generate");
                    else Debug.Log("Incremental #" + m_IncrementalCounter);

                    m_Gen.AddResult(result, m_IncrementalCounter);
                    
                    m_IncrementalCounter++;
                }
            }
        }

        public void NativeOnGUI()
        {
            var dt = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton().DeltaTime;
            
            GUILayout.BeginVertical();
            GUILayout.Label($"1/DT: {(1 / Time.deltaTime):F0}");
            GUILayout.Label($"1/TD: {(1 / dt):F0}");
            GUILayout.Label($"Snapshot size: {m_Gen.Size}");
            GUILayout.EndVertical();
        }
    }*/
}