using System;
using DefaultNamespace;
using LiteNetLib.Utils;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using Unity.Entities;

namespace package.stormium.def.characters
{
    public enum CharacterState
    {
        /// <summary>
        /// The character is alive and spawned
        /// </summary>
        Spawned,
        /// <summary>
        /// The character is unspawned, may be caused by elimination
        /// </summary>
        Unspawned
    }
    
    public class DefGameCharacterSystemNet : IDisposable
    {
        private World m_ActiveWorld;

        public MessageIdent MsgUpdateStatus = new MessageIdent($"{nameof(DefGameCharacterSystemNet)}.UpdateStatus");
        public MessageIdent MsgCreate = new MessageIdent($"{nameof(DefGameCharacterSystemNet)}.Create");
        public MessageIdent MsgRemove = new MessageIdent($"{nameof(DefGameCharacterSystemNet)}.Remove");
        
        public DefGameCharacterSystemNet(World world)
        {
            m_ActiveWorld = world;

            var patternManager = m_ActiveWorld.GetOrCreateManager<MsgIdRegisterSystem>();
            patternManager.Register(MsgUpdateStatus);
            patternManager.Register(MsgCreate);
            patternManager.Register(MsgRemove);
        }

        public void Dispose()
        {
            m_ActiveWorld = null;
        }

        public NetDataWriter NewCreateCharacterMessage(Entity entity, CharacterState state)
        {
            var gameServerManagement = m_ActiveWorld.GetOrCreateManager<GameServerManagement>();
            var msgManager           = gameServerManagement.Main.LocalInstance.GetMessageManager();

            var dataWriter = msgManager.Create(MsgCreate);
            dataWriter.Put(entity);
            dataWriter.Put((byte) state);

            return dataWriter;
        }
        
        public NetDataWriter NewRemoveCharacterMessage(Entity entity, CharacterState state)
        {
            var gameServerManagement = m_ActiveWorld.GetOrCreateManager<GameServerManagement>();
            var msgManager           = gameServerManagement.Main.LocalInstance.GetMessageManager();

            var dataWriter = msgManager.Create(MsgRemove);
            dataWriter.Put(entity);
            dataWriter.Put((byte) state);

            return dataWriter;
        }
        
        public NetDataWriter CreateNewStatusMessage(Entity entity, CharacterState state)
        {
            var gameServerManagement = m_ActiveWorld.GetOrCreateManager<GameServerManagement>();
            var msgManager           = gameServerManagement.Main.LocalInstance.GetMessageManager();

            var dataWriter = msgManager.Create(MsgUpdateStatus);
            dataWriter.Put(entity);
            dataWriter.Put((byte) state);

            return dataWriter;
        }
    }
}