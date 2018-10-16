using LiteNetLib.Utils;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStVelocityProcessOnCharacterControllerSystem))]
    public class DefStVelocityProcessSystem : GameComponentSystem,
                                              EventReceiveData.IEv
    {
        // -------------------------------------------------------- //
        // Static fields
        // -------------------------------------------------------- //
        public static readonly MessageIdent MsgUpdateVelocity;
        public static readonly MessageIdent MsgMassUpdateVelocity;

        // -------------------------------------------------------- //
        // Groups
        // -------------------------------------------------------- //
        struct VelocityGroup
        {
            public ComponentDataArray<StVelocity> VelocityArray;
            public ComponentDataArray<NetworkEntity> NetworkArray;
            public EntityArray Entities;

            public readonly int Length;
        }

        [Inject] private VelocityGroup m_VelocityGroup;

        // -------------------------------------------------------- //
        // Fields
        // -------------------------------------------------------- //
        private int           m_WriterSize;
        private NetDataWriter m_NetDataWriter;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            
            m_WriterSize = MessageIdent.HeaderSize + (sizeof(int) * 2) + UnsafeUtility.SizeOf<float3>();
        }

        protected override void OnUpdate()
        {
            if (!GameServerManagement.IsCurrentlyHosting)
                return;
            
            for (int i = 0; i != m_VelocityGroup.Length; i++)
            {
                ResetMessage();
                
                var packet = new VelocityPacket(m_VelocityGroup.Entities[i], m_VelocityGroup.VelocityArray[i].Value);
                GameServerManagement.Main.ServerInstance.GetMessageManager().Create(MsgUpdateVelocity, m_NetDataWriter);
                packet.WriteTo(m_NetDataWriter);
                ServerSendToAll(m_NetDataWriter);
            }
        }

        private void ResetMessage()
        {
            m_NetDataWriter = new NetDataWriter(true, m_WriterSize);
        }

        private void SetFromPacket(VelocityPacket packet, ConnectionEntityManager conEntityMgr)
        {
            var entity = packet.Entity;

            if (!conEntityMgr.HasEntity(entity))
            {
                Debug.LogError($"No entity {entity.Index}, {entity.Version} found");
                return;
            }

            entity = conEntityMgr.GetEntity(entity);
                
            entity.SetOrAddComponentData(new StVelocity(packet.Velocity));
        }

        // -------------------------------------------------------- //
        // Callbacks
        // -------------------------------------------------------- //
        public void Callback(EventReceiveData.Arguments args)
        {
            if (args.Reader.Type != MessageType.Pattern || args.Caller.SelfHost)
                return;

            var caller       = args.Caller;
            var peerInstance = args.PeerInstance;
            var reader       = args.Reader;

            var conPatternMgr = peerInstance.GetPatternManager();
            var conEntityMgr  = peerInstance.Get<ConnectionEntityManager>();

            var msgId = conPatternMgr.GetPattern(reader);
            if (msgId == MsgUpdateVelocity)
            {
                var packet = new VelocityPacket(reader.Data);
                SetFromPacket(packet, conEntityMgr);
            }
            else if (msgId == MsgMassUpdateVelocity)
            {
                var length = reader.Data.GetInt();
                for (int i = 0; i != length; i++)
                {
                    var packet = new VelocityPacket(reader.Data);
                    SetFromPacket(packet, conEntityMgr);
                }
            }
        }

        public struct VelocityPacket
        {
            public float  Timestamp;
            public Entity Entity;
            public float3 Velocity;

            public VelocityPacket(Entity entity, float3 velocity)
            {
                Timestamp = Time.time;
                Entity    = entity;
                Velocity  = velocity;
            }

            public VelocityPacket(NetDataReader reader)
            {
                Timestamp = reader.GetFloat();
                Entity    = reader.GetEntity();
                Velocity  = reader.GetVec3();
            }

            public void WriteTo(NetDataWriter writer)
            {
                writer.Put(Timestamp);
                writer.Put(Entity);
                writer.Put(Velocity);
            }
        }
    }
}