using System;
using System.Net;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.shared;
using Runtime;
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
    //[DisableAutoCreation]
    public class TestDmMultiPlayer : ComponentSystem, INativeEventOnGUI
    {
        public StormiumGameServerManager ServerMgr;
        public AppEventSystem AppEventSystem;
        public Entity         GameModeEntity;
        
        // Network related...
        public string HostAddr = "127.0.0.1";
        public int    HostPort = 8590;

        protected override void OnCreateManager()
        {
            ServerMgr = World.GetOrCreateManager<StormiumGameServerManager>();
            AppEventSystem = World.GetOrCreateManager<AppEventSystem>(); 

            AppEventSystem.SubscribeToAll(this);
             
            Application.targetFrameRate = 150;
        }

        protected override unsafe void OnUpdate()
        {
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

                if (!EntityManager.Exists(ServerMgr.HostEntity))
                    DoConnectAndCreate();
                else
                {
                    DoCancelOrStop();
                }
            }
        }


        public void DoConnectAndCreate()
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
                if (!ServerMgr.ConnectToServer(targetEp))
                    Debug.LogError("Couldn't connect to a server. endpoint=" + targetEp);
            }

            if (GUILayout.Button("Create"))
            {
                Application.targetFrameRate = 100;

                if (!ServerMgr.LaunchServer(HostPort))
                    Debug.LogError("Couldn't launch a server. port=" + HostPort);

                GameModeEntity = EntityManager.CreateEntity
                (
                    ComponentType.Create<DeathMatchData>(),
                    ComponentType.Create<EntityAuthority>()
                );

#if UNITY_EDITOR
                EntityManager.SetName(GameModeEntity, "DeathMatch GameMode");
#endif
            }
        }

        public void DoCancelOrStop()
        {
            var connectedInstanceBuffer = EntityManager.GetBuffer<ConnectedInstance>(ServerMgr.HostEntity);

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
            GUILayout.Space(5);
            
            var isValid = EntityManager.HasComponent(ServerMgr.HostEntity, typeof(ValidInstanceTag));
            if (GUILayout.Button(isValid ? "Stop" : "Cancel"))
            {
                ServerMgr.StopEverything();
            }
        }
    }
}