using System;
using System.Net;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.shared;
using Stormium.Core;
using Stormium.Default.GameModes;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Stormium.Default.Tests 
{
    public static unsafe class TestComp<T>
        where T : unmanaged
    {
        public unsafe delegate void u(void* d, void* r);
        
        public static void TestGeneric(void* data, void* result)
        {
            UnsafeUtility.MemCpy(result, data, sizeof(T));
        }
    }
    
    public class TestDmMultiPlayer : ComponentSystem, INativeEventOnGUI
    {
        public NetworkManager NetworkMgr;
        public AppEventSystem AppEventSystem;
        public Entity         GameModeEntity;
        
        // Network related...
        public Entity HostEntity;
        public string HostAddr = "127.0.0.1";
        public int    HostPort = 8590;

        protected override void OnCreateManager()
        {
            NetworkMgr     = World.GetOrCreateManager<NetworkManager>();
            AppEventSystem = World.GetOrCreateManager<AppEventSystem>(); 

            AppEventSystem.SubscribeToAll(this);

            Application.targetFrameRate = 64;
        }

        delegate void k<T>(T value, out T nv) where T : struct;
        delegate void n(int value, out int nv);
        public unsafe delegate void u(void* d, void* r);

        protected override unsafe void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                var dg = BurstCompiler.CompileDelegate<k<int>>(Test);
                dg(8, out var val);
                Debug.Log(val);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                var dg = BurstCompiler.CompileDelegate<TestComp<int>.u>(TestComp<int>.TestGeneric);
                using (var value = new UnsafeAllocation<int>(Allocator.Temp, 8))
                using (var result = new UnsafeAllocation<int>(Allocator.Temp, 16))
                {
                    dg(value.Data, result.Data);

                    UnsafeUtility.CopyPtrToStructure(result.Data, out int resultNumber);
                    
                    Debug.Log(resultNumber);
                }
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                var dg = BurstCompiler.CompileDelegate<n>(TestNoGeneric);
                dg(8, out var val);
                Debug.Log(val);
            }
        }
        
        public static void TestNoGeneric(int value, out int newVal)
        {
            newVal = value;
        }

        public static void Test<T>(T value, out T newVal)
            where T : struct
        {
            newVal = value;
        }

        public void NativeOnGUI()
        {
            var gameTime = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
            
            using (new GUILayout.VerticalScope())
            {
                GUI.contentColor = Color.black;

                GUILayout.Label("TestSystem Actions:");
                GUILayout.Space(1);
                GUILayout.Label($"DT={gameTime.DeltaTick}ms");
                
                var networkMgr = World.GetExistingManager<NetworkManager>();

                if (!EntityManager.Exists(HostEntity))
                    DoConnectAndCreate(networkMgr);
                else
                {
                    DoCancelOrStop(networkMgr);
                }
            }
        }


        public void DoConnectAndCreate(NetworkManager networkMgr)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Server Address: ");
                HostAddr = GUILayout.TextField(HostAddr);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Server Port:   ");
                if (int.TryParse(GUILayout.TextField(HostPort.ToString()), out HostPort))
                {
                }
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Connect"))
            {
                var targetEp = new IPEndPoint(IPAddress.Parse(HostAddr), HostPort);
                var r = networkMgr.StartClient(targetEp);

                HostEntity = r.ClientInstanceEntity;
            }

            if (GUILayout.Button("Create"))
            {
                var localEp = new IPEndPoint(IPAddress.Any, HostPort);
                var r       = networkMgr.StartServer(localEp);

                HostEntity = r.Entity;
                
                GameModeEntity = EntityManager.CreateEntity
                (
                    ComponentType.Create<DeathMatchData>(),
                    ComponentType.Create<SimulateEntity>()
                );
                
#if UNITY_EDITOR
                EntityManager.SetName(GameModeEntity, "DeathMatch GameMode");
#endif
            }
        }

        public void DoCancelOrStop(NetworkManager networkMgr)
        {
            var isValid = EntityManager.HasComponent(HostEntity, typeof(ValidInstanceTag));
            if (GUILayout.Button(isValid ? "Stop" : "Cancel"))
            {
                HostEntity = default;
                networkMgr.StopAll();
            }

            if (EntityManager.Exists(HostEntity))
            {
                var connectedInstanceBuffer = EntityManager.GetBuffer<ConnectedInstance>(HostEntity);

                GUILayout.Space(5);
                for (var i = 0; i != connectedInstanceBuffer.Length; i++)
                {
                    var connectedInstance = connectedInstanceBuffer[i];
                    if (!EntityManager.Exists(connectedInstance.Entity) || !EntityManager.HasComponent(connectedInstance.Entity, typeof(NetworkInstanceData)))
                        continue;

                    var data = EntityManager.GetComponentData<NetworkInstanceData>(connectedInstance.Entity);
                    var cs   = data.Commands.ConnectionStatus;
                    GUILayout.Label($"Id={connectedInstance.Connection.Id}, Type={data.InstanceType}, Ping={cs.ping}, InKB/s={cs.inBytesPerSecond * 0.0005f}, OutKB/s={cs.outBytesPerSecond * 0.0005f}");
                }
            }
        }
    }
}