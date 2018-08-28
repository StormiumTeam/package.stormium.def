using System;
using package.stormiumteam.networking.plugins;
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
            public SubtractiveComponent<NetSnapshotPosition>                   Sub1;
            public SubtractiveComponent<NetPositionInterpolator>               Sub2;
            public SubtractiveComponent<NetPositionInterpolatorBuffer>         Sub3;
            public TransformAccessArray                                        Trs;

            public readonly int Length;
        }

        public struct PositionInterpolatorGroup
        {
            public ComponentDataArray<NetworkEntity>                           N1;
            public ComponentDataArray<Position>                                Local;
            public ComponentDataArray<NetPosition>                             Network;
            public ComponentDataArray<NetPositionInterpolator> Interpolator;
            public BufferArray<NetPositionInterpolatorBuffer>                  BufferArray;
            public SubtractiveComponent<VoidSystem<DefaultNetTransformSystem>> Void1;
            public TransformAccessArray                                        Trs;

            public readonly int Length;
        }
        
        public struct PositionInterpolatorGroupWithoutBuffer
        {
            public ComponentDataArray<NetworkEntity>                           N1;
            public ComponentDataArray<Position>                                Local;
            public ComponentDataArray<NetPosition>                             Network;
            public ComponentDataArray<NetPositionInterpolator>                 Interpolator;
            public SubtractiveComponent<VoidSystem<DefaultNetTransformSystem>> Void1;
            public SubtractiveComponent<NetPositionInterpolatorBuffer> Sub1;
            public TransformAccessArray                                        Trs;
            public EntityArray Entities;

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

        [Inject] private PositionInterpolatorGroupWithoutBuffer m_WithoutBufferGroup;
        [Inject] private PositionInterpolatorGroup m_BufferInterpolatorGroup;
        [Inject] private PositionGroup m_PositionGroup;
        [Inject] private RotationGroup m_RotationGroup;
        [Inject] private GameServerManagement m_Gms;

        protected override void OnUpdate()
        {
            var delta = Time.deltaTime;

            var needToReinject = false;
            for (int i = 0; i != m_WithoutBufferGroup.Length; i++)
            {
                EntityManager.AddBuffer<NetPositionInterpolatorBuffer>(m_WithoutBufferGroup.Entities[i]);
                needToReinject = true;
            }
            if (needToReinject) UpdateInjectedComponentGroups();
            
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

            for (int i = 0; i != m_BufferInterpolatorGroup.Length; i++)
            {
                const float cDelay = 0.01f;

                var interpolator = m_BufferInterpolatorGroup.Interpolator[i];
                var elemBuffer   = m_BufferInterpolatorGroup.BufferArray[i];

                var target = m_BufferInterpolatorGroup.Trs[i];

                //Update time
                if (interpolator.CurrentTime >= 0f)
                {
                    //If we had time, we need to move forward by adding the delta time
                    //From the last frame.
                    interpolator.CurrentTime += delta;
                }

                //If we have elements, we need to start the interpolation
                if (elemBuffer.Length > 0)
                {
                    //This is E(n-1)
                    var prev = elemBuffer[0];

                    //If we don't have a time set, set the time to the oldest interp entry
                    //The oldest entry is the first in the queue.
                    if (interpolator.CurrentTime < 0f)
                    {
                        interpolator.CurrentTime = Math.Max(prev.Timestamp - cDelay, 0f);

                        //Set the time this interpolation piece has started from
                        interpolator.StartedTime = interpolator.CurrentTime;

                        //Set starting points to interpolate from
                        interpolator.StartingPosition = target.position;
                    }

                    //Current position is towards the first element of the queue
                    //This means, since the first frame, the interpolation is still towards the first entry
                    if (interpolator.CurrentTime < prev.Timestamp)
                    {
                        interpolator.TimeBetween       = prev.Timestamp - interpolator.StartedTime;
                        interpolator.TimeSincePrevious = interpolator.CurrentTime - interpolator.StartedTime;

                        if (Math.Abs(prev.Timestamp - interpolator.CurrentTime) > 0.1f
                            || interpolator.TimeBetween > 0.1f
                            || Mathf.Abs(interpolator.LatestTimestamp - interpolator.CurrentTime) > 0.1f)
                        {
                            interpolator = default(NetPositionInterpolator);
                            interpolator.CurrentTime = prev.Timestamp;
                            elemBuffer.Clear();
                        }

                        target.position = Vector3.Lerp(interpolator.StartingPosition, prev.Position, interpolator.TimeSincePrevious / interpolator.TimeBetween);
                        m_BufferInterpolatorGroup.Local[i] = new Position {Value = target.position};
                        m_BufferInterpolatorGroup.Interpolator[i] = interpolator;
                        continue;
                    }

                    //This is E(n)
                    var next = prev;

                    //Find the first entry where the current time doesn't pass it.
                    while (interpolator.CurrentTime > next.Timestamp && elemBuffer.Length > 0)
                    {
                        if (elemBuffer.Length == 1)
                        {
                            next = elemBuffer[0];
                            elemBuffer.RemoveAt(0); // FIFO
                            prev = next;
                        }
                        else
                        {
                            prev = elemBuffer[0];
                            next = elemBuffer[1];
                            
                            elemBuffer.RemoveAt(0); // FIFO
                        }
                    }

                    //Calculate T0+dt since prev instead of since begining
                    interpolator.TimeBetween = interpolator.CurrentTime - prev.Timestamp;
                    
                    if (interpolator.TimeBetween > 0.1f)
                    {
                        interpolator = default(NetPositionInterpolator);
                        interpolator.CurrentTime = prev.Timestamp;
                        elemBuffer.Clear();
                        
                        target.position                    = Vector3.Lerp(prev.Position, next.Position, (interpolator.TimeSincePrevious / interpolator.TimeBetween));
                        m_BufferInterpolatorGroup.Local[i] = new Position {Value = target.position};
                        m_BufferInterpolatorGroup.Interpolator[i] = interpolator;

                        continue;
                    }

                    if (interpolator.TimeBetween == 0)
                    {
                        interpolator.TimeBetween = 1;
                        interpolator.TimeSincePrevious = 1;
                    }

                    //Return the interpolation of T0+dt in this piece
                    target.position = Vector3.Lerp(prev.Position, next.Position, (interpolator.TimeSincePrevious / interpolator.TimeBetween));
                    m_BufferInterpolatorGroup.Local[i] = new Position {Value = target.position};

                    interpolator.StartedTime = interpolator.CurrentTime;
                    interpolator.StartingPosition = prev.Position;

                    m_BufferInterpolatorGroup.Interpolator[i] = interpolator;
                }
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
                msg.Put(Time.time);
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
                var timestamp = args.Reader.Data.GetFloat();
                var position = args.Reader.Data.GetVec3();

                if (!entity.HasComponent<NetSnapshotPosition>())
                {
                    Vector3 oldPosition = entity.GetComponentData<NetPosition>().Target;
                    var movDir = (position - oldPosition).normalized * 0.003f;
                    
                    var predictedPosition = position + (movDir * args.PeerInstance.Get<ConnectionNetManagerConfig>().ConfigUpdateTime);
                    var oldProgress = entity.GetComponentData<NetPosition>().DeltaProgress;
                    entity.SetOrAddComponentData(new NetPosition
                    {
                        Target = position, 
                        Predicted = predictedPosition,
                        DeltaProgress = Mathf.Lerp(oldProgress, 0, 0.5f)
                    });

                    if (entity.HasComponent<NetPositionInterpolatorBuffer>() && entity.HasComponent<NetPositionInterpolator>())
                    {
                        var bufferArray = EntityManager.GetBuffer<NetPositionInterpolatorBuffer>(entity);
                        bufferArray.Add(new NetPositionInterpolatorBuffer(timestamp, Time.time, position));

                        var interpolator = EntityManager.GetComponentData<NetPositionInterpolator>(entity);
                        interpolator.LatestPosition = position;
                        interpolator.LatestTimestamp = timestamp;
                        EntityManager.SetComponentData(entity, interpolator);
                    }
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

                entity.SetOrAddComponentData(new NetRotation() {Value = math.quaternion(x, y, z, w)});
            }
        }
    }
}