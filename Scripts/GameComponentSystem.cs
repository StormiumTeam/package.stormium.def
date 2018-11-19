using System;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.core;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Scripts;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    public abstract class GameComponentSystem : ComponentSystem
    {
        protected ConnectionEntityManager ServerEntityMgr => GameServerManagement
                                                             .Main
                                                             .ServerInstance
                                                             .World
                                                             .GetOrCreateManager<ConnectionEntityManager>();

        protected bool IsConnectedOrHosting => GameServerManagement.Main?.LocalNetManager.IsRunning ?? false;
        protected bool CanExecuteServerActions => !IsConnectedOrHosting || GameServerManagement.IsCurrentlyHosting;

        [Inject] protected MsgIdRegisterSystem MsgIdRegisterSystem;
        [Inject] protected GameServerManagement GameServerManagement;
        [Inject] protected AppEventSystem AppEventSystem;
        
        protected override void OnCreateManager()
        {
            MsgIdRegisterSystem.Register(this);
            AppEventSystem.SubscribeToAll(this);
        }

        protected NetDataWriter CreateMessage(MessageIdent messageIdent)
        {
            if (IsConnectedOrHosting)
            {
                return GameServerManagement.Main.LocalInstance.GetMessageManager().Create(messageIdent);
            }

            return null;
        }

        protected void ServerSendToPeer(NetPeer peer, NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            peer.Send(data, deliveryMethod);
        }

        protected void ServerSendToAll(NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            var manager = GameServerManagement.Main.ServerInstance.GetDefaultChannel().Manager;
            manager.SendToAll(data, deliveryMethod);
        }

        protected void SendToServer(NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            var manager = GameServerManagement.Main.LocalNetManager;
            manager.SendToAll(data, deliveryMethod);
        }
        
        public Entity CreateCommand(ComponentType header, params ComponentType[] cmdTypes)
        {
            var initArray = new[] {header, typeof(EntityCommand)};
            
            var entity = EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
            EntityManager.SetComponentData(entity, new EntityCommand() {HeaderTypeIndex = header.TypeIndex});
            return entity;
        }
        
        public Entity CreateCommandTarget(ComponentType header, params ComponentType[] cmdTypes)
        {
            var initArray = new[] {header, typeof(EntityCommand), typeof(EntityCommandTarget)};
            
            var entity = EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
            EntityManager.SetComponentData(entity, new EntityCommand() {HeaderTypeIndex = header.TypeIndex});
            return entity;
        }
        
        public Entity CreateCommandTs(ComponentType header, params ComponentType[] cmdTypes)
        {
            var initArray = new[] {header, typeof(EntityCommand), typeof(EntityCommandSource), typeof(EntityCommandTarget)};
            
            var entity = EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
            EntityManager.SetComponentData(entity, new EntityCommand() {HeaderTypeIndex = header.TypeIndex});
            return entity;
        }
        
        public Entity CreateCommandResult(params ComponentType[] cmdTypes)
        {
            var initArray = new ComponentType[] {typeof(EntityCommand), typeof(EntityCommandResult)};
            
            return EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
        }

        public void DiffuseCommand(Entity command, Entity commandResult, bool defaultResult, CmdState state)
        {
            commandResult.SetComponentData(new EntityCommandResult { IsAuthorized = Convert.ToByte(defaultResult) });
            
            foreach (var ev in AppEvent<StEventDiffuseCommand.IEv>.eventList)
            {
                AppEvent<StEventDiffuseCommand.IEv>.Caller = this;

                ev.OnCommandDiffuse(new StEventDiffuseCommand.Arguments(command, commandResult, state));
            }
        }

        public bool GetCmdResult(Entity cmdResultEntity)
        {
            return EntityManager.GetComponentData<EntityCommandResult>(cmdResultEntity).AsBool();
        }

        public void BroadcastNewEntity(EntityCommandBuffer buffer, bool removeOnCompletion)
        {
            var id = -1;
            if (IsConnectedOrHosting)
            {
                id = GameServerManagement.Main.LocalInstance.Id;
            }
            
            buffer.CreateEntity();
            buffer.AddComponent(new BroadcastEntityComponentsOnce(id));
            if (removeOnCompletion)
            {
                buffer.AddComponent(new BroadcastEntityComponentsThenDestroyIt());
            }
        }
        
        public void BroadcastNewEntity(EntityCommandBuffer buffer, EntityArchetype archetype, bool removeOnCompletion)
        {
            var id = -1;
            if (IsConnectedOrHosting)
            {
                id = GameServerManagement.Main.LocalInstance.Id;
            }
            
            buffer.CreateEntity(archetype);
            buffer.AddComponent(new BroadcastEntityComponentsOnce(id));
            if (removeOnCompletion)
            {
                buffer.AddComponent(new BroadcastEntityComponentsThenDestroyIt());
            }
        }

        public Entity GetEntity(Entity entity)
        {
            if (IsConnectedOrHosting)
            {
                return ServerEntityMgr.GetEntity(entity);
            }

            return entity;
        }
    }
    
    public abstract class GameJobComponentSystem : JobComponentSystem
    {
        protected ConnectionEntityManager ServerEntityMgr => GameServerManagement
                                                             .Main
                                                             .ServerInstance
                                                             .World
                                                             .GetOrCreateManager<ConnectionEntityManager>();

        protected bool IsConnectedOrHosting => GameServerManagement.Main?.LocalNetManager.IsRunning ?? false;
        
        [Inject] protected MsgIdRegisterSystem  MsgIdRegisterSystem;
        [Inject] protected GameServerManagement GameServerManagement;
        [Inject] protected AppEventSystem       AppEventSystem;
        
        protected override void OnCreateManager()
        {
            MsgIdRegisterSystem.Register(this);
            AppEventSystem.SubscribeToAll(this);
        }

        protected void ServerSendToPeer(NetPeer peer, NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            peer.Send(data, deliveryMethod);
        }

        protected void ServerSendToAll(NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            var manager = GameServerManagement.Main.ServerInstance.GetDefaultChannel().Manager;
            manager.SendToAll(data, deliveryMethod);
        }
    }
}