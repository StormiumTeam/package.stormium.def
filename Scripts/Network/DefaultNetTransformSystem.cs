using DefaultNamespace;
using LiteNetLib;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;
using Quaternion = UnityEngine.Quaternion;

namespace package.stormium.def.Network
{
    public class DefaultNetTransformConstants : ComponentSystem
    {
        public static readonly MessageIdent MsgUpdatePosition;
        public static readonly MessageIdent MsgUpdateRotation;

        [Inject] private MsgIdRegisterSystem m_MsgIdRegisterSystem;

        protected override void OnCreateManager(int capacity)
        {
            m_MsgIdRegisterSystem.Register(this);
        }

        protected override void OnUpdate()
        {

        }
    }

    // TODO: implement scale from preview.10
    [UpdateAfter(typeof(PreLateUpdate))]
    public class DefaultNetTransformSystem : ComponentSystem
    {
        public struct PositionGroup
        {
            public ComponentDataArray<NetworkEntity>                           N1;
            public ComponentDataArray<Position>                                Local;
            public ComponentDataArray<NetPosition>                             Network;
            public SubtractiveComponent<VoidSystem<DefaultNetTransformSystem>> Void1;
            public SubtractiveComponent<NetSnapshotPosition> Sub1;
            public TransformAccessArray Trs;

            public readonly int Length;
        }

        public struct DeltaPositionGroup
        {
            public ComponentDataArray<NetworkEntity>                           N1;
            public ComponentDataArray<Position>                                Local;
            public ComponentDataArray<NetPosition> Predicted;
            [ReadOnly] public SharedComponentDataArray<NetSnapshotPosition>               Network;
            public SubtractiveComponent<VoidSystem<DefaultNetTransformSystem>> Void1;
            public TransformAccessArray                                        Trs;

            public readonly int Length;
        }

        public struct RotationGroup
        {
            public ComponentDataArray<NetworkEntity>                           N1;
            public ComponentDataArray<Rotation>                                Local;
            public ComponentDataArray<NetRotation>                             Network;
            public SubtractiveComponent<VoidSystem<DefaultNetTransformSystem>> Void1;
            public TransformAccessArray Trs;

            public readonly int Length;
        }

        [Inject] private DeltaPositionGroup m_DeltaPositionGroup;
        [Inject] private PositionGroup m_PositionGroup;
        [Inject] private RotationGroup m_RotationGroup;
        [Inject] private GameServerManagement m_Gms;

        protected override void OnUpdate()
        {
            var delta = Time.deltaTime;
            for (int i = 0; i != m_PositionGroup.Length; i++)
            {
                var localLocation  = m_PositionGroup.Local[i].Value;
                var serverLocation = m_PositionGroup.Network[i].Target;
                
                var latency = 15f;
                var deltaProgress = m_PositionGroup.Network[i].DeltaProgress;
                
                var progress = math.clamp((deltaProgress / (math.max(latency, 10) * 0.01f)) * 5f, 0, 0.99f);
                localLocation = Vector3.Lerp(localLocation, m_PositionGroup.Network[i].Predicted, progress);
                //localLocation = Vector3.MoveTowards(localLocation, m_PositionGroup.Network[i].Predicted, 10 * Time.deltaTime);

                var net = m_PositionGroup.Network[i];
                net.DeltaProgress = deltaProgress + Time.deltaTime;
                m_PositionGroup.Network[i] = net;

                m_PositionGroup.Local[i] = new Position {Value = localLocation};
                m_PositionGroup.Trs[i].position = localLocation;
            }
            
            for (int i = 0; i != m_DeltaPositionGroup.Length; i++)
            {
                var netDelta = m_DeltaPositionGroup.Network[i];
                var predComp = m_DeltaPositionGroup.Predicted[i];
                var history = netDelta.DeltaPositions;

                predComp.DeltaProgress += delta;

                var latency = 15;

                var finalPosition = predComp.Predicted * latency * 0.05f;
                var t = delta / (latency * (1 + 0.05f));
                finalPosition = (finalPosition - m_DeltaPositionGroup.Local[i].Value) * Time.deltaTime;

                m_DeltaPositionGroup.Local[i] = new Position {Value = finalPosition};
                m_DeltaPositionGroup.Trs[i].position = finalPosition;
                m_DeltaPositionGroup.Predicted[i] = predComp;
            }

            for (int i = 0; i != m_RotationGroup.Length; i++)
            {
                var localLocation  = m_RotationGroup.Local[i].Value;
                var serverLocation = m_RotationGroup.Network[i].Value;
                var distance       = Quaternion.Angle(localLocation, serverLocation);
                if (distance > 25f) // Teleport
                {
                    localLocation = serverLocation;
                }
                else
                {
                    var clampLatency = 15; // todo: clamp(100 - playerLatency, 0, 100)
                    var speed        = math.max((clampLatency * 0.1f) + 1f, 6);
                    var ogDistance   = math.max(distance, 0.3f);
                    distance++;
                    speed *= math.clamp((distance * distance * distance * distance) * ogDistance, 2.5f, speed) * 7.5f;

                    localLocation = Quaternion.RotateTowards(localLocation, serverLocation, delta * speed);
                }

                m_RotationGroup.Local[i] = new Rotation {Value = localLocation};
                m_RotationGroup.Trs[i].rotation = localLocation;
            }
        }
    }

    public class DefaultNetSyncTransformDataSystem : ComponentSystem,
        EventReceiveData.IEv
    {
        public struct PositionGroup
        {
            public ComponentDataArray<NetworkEntity>                                   N1;
            public ComponentDataArray<Position>                                        Local;
            public ComponentDataArray<NetPosition>                                     Network;
            public SubtractiveComponent<VoidSystem<DefaultNetSyncTransformDataSystem>> Void1;

            public readonly int Length;
        }

        public struct RotationGroup
        {
            public ComponentDataArray<NetworkEntity>                                   N1;
            public ComponentDataArray<Rotation>                                        Local;
            public ComponentDataArray<NetRotation>                                     Network;
            public SubtractiveComponent<VoidSystem<DefaultNetSyncTransformDataSystem>> Void1;

            public readonly int Length;
        }

        [Inject] private AppEventSystem m_AppEventSystem;
        [Inject] private GameServerManagement m_GameServerManagement;
        [Inject] private PositionGroup        m_PositionGroup;
        [Inject] private RotationGroup        m_RotationGroup;

        protected override void OnCreateManager(int capacity)
        {
            m_AppEventSystem.SubscribeToAll(this);
        }

        protected override void OnUpdate()
        {
            if (!m_GameServerManagement.IsCurrentlyHosting) return;

            var manager = m_GameServerManagement.Main.LocalNetManager;
            var netInstance = m_GameServerManagement.Main.LocalInstance;
            var msgMgr = netInstance.GetMessageManager();
            
            for (int i = 0; i != m_PositionGroup.Length; i++)
            {
                var netEntity = m_PositionGroup.N1[i];
                if (netEntity.InstanceId != netInstance.Id)
                    continue;
                
                var msg = msgMgr.Create(DefaultNetTransformConstants.MsgUpdatePosition);
                msg.Put(new Entity() { Index = netEntity.NetId,  Version = netEntity.NetVersion});
                msg.Put(m_PositionGroup.Local[i].Value);
                
                manager.SendToAll(msg, DeliveryMethod.Unreliable);
            }
            
            for (int i = 0; i != m_RotationGroup.Length; i++)
            {
                var netEntity = m_PositionGroup.N1[i];
                if (netEntity.InstanceId != netInstance.Id)
                    continue;
                var val = m_RotationGroup.Local[i].Value;
                
                var msg = msgMgr.Create(DefaultNetTransformConstants.MsgUpdateRotation);
                msg.Put(new Entity() { Index = netEntity.NetId,  Version = netEntity.NetVersion});
                msg.Put(val.value.x);
                msg.Put(val.value.y);
                msg.Put(val.value.z);
                msg.Put(val.value.w);
                
                manager.SendToAll(msg, DeliveryMethod.Unreliable);
            }
        }

        void EventReceiveData.IEv.Callback(EventReceiveData.Arguments args)
        {
            if (args.Reader.Type != MessageType.Pattern)
                return;
            
            var conMsgMgr = args.PeerInstance.GetPatternManager();
            var conEntityMgr = args.PeerInstance.Get<ConnectionEntityManager>();
            
            var msg = conMsgMgr.GetPattern(args.Reader);
            if (msg == DefaultNetTransformConstants.MsgUpdatePosition)
            {
                var entity = conEntityMgr.GetEntity(args.Reader.GetEntity());
                var position = args.Reader.Data.GetVec3();

                if (!entity.HasComponent<NetSnapshotPosition>())
                {
                    Vector3 oldPosition = entity.GetComponentData<NetPosition>().Target;
                    var movDir = (position - oldPosition).normalized * 0.003f;
                    /*if ((position - oldPosition).magnitude < 0.1f)
                        movDir = Vector3.zero;*/
                    
                    var predictedPosition = position + (movDir * args.PeerInstance.Get<ConnectionNetManagerConfig>().ConfigUpdateTime);
                    var oldProgress = entity.GetComponentData<NetPosition>().DeltaProgress;
                    entity.SetOrAddComponentData(new NetPosition
                    {
                        Target = position, 
                        Predicted = predictedPosition,
                        DeltaProgress = Mathf.Lerp(oldProgress, 0, 0.5f)
                    });
                }
                else
                {
                    var netDelta   = EntityManager.GetSharedComponentData<NetSnapshotPosition>(entity);
                    var history    = netDelta.DeltaPositions;
                    var dtDuration = entity.GetComponentData<NetPosition>().DeltaProgress;
                    while (history.Count > 0 && dtDuration > 0)
                    {
                        if (dtDuration >= history[0].Delta)
                        {
                            dtDuration -= history[0].Delta;
                            history.RemoveAt(0);
                        }
                        else
                        {
                            var t     = 1 - dtDuration / history[0].Delta;
                            var frame = history[0];
                            frame.Delta -= dtDuration;
                            frame.Value *= t;
                            history[0]  =  frame;
                        }
                    }
                    
                    var predictedPosition = (float3)position;
                    foreach (var frame in history)
                    {
                        predictedPosition += frame.Value;
                    }
                    
                    entity.SetOrAddComponentData(new NetPosition {Target = position, Predicted = predictedPosition, DeltaProgress = dtDuration});
                }
            }
            else if (msg == DefaultNetTransformConstants.MsgUpdateRotation)
            {
                var entity   = conEntityMgr.GetEntity(args.Reader.GetEntity());
                var x = args.Reader.Data.GetFloat();
                var y = args.Reader.Data.GetFloat();
                var z = args.Reader.Data.GetFloat();
                //var w = math.sqrt(1 - (x * x) - (y * y) - (z * z));
                var w = args.Reader.Data.GetFloat();

                entity.SetOrAddComponentData(new NetRotation() {Value = math.Quaternion(x, y, z, w)});
            }
        }
    }
}