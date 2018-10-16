using System;
using System.ComponentModel;
using System.Text;
using package.stormiumteam.networking.plugins;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace package.stormium.def.characters
{
    public class DefGameCharacterSystem : ComponentSystem
    {
        #region Barriers

        [UpdateAfter(typeof(DefGameCharacterSystem))]
        class Barrier : BarrierSystem
        {
        }

        #endregion

        #region Groups

        /// <summary>
        /// All characters linked to a player
        /// </summary>
        public struct PlayerGroup
        {

        }

        public struct Characters
        {
            public ComponentDataArray<CharacterTag> CharacterTags;
            public EntityArray                      Entities;

            public readonly int Length;
        }

        public struct PlayerCharacters
        {
            public ComponentDataArray<CharacterTag>         CharacterTags;
            public ComponentDataArray<CharacterPlayerOwner> Owners;
            public EntityArray                              Entities;

            public readonly int Length;
        }

        #endregion

        [Inject] private MsgIdRegisterSystem  m_MsgIdRegisterSystem;
        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;
        [Inject] private GameServerManagement m_GameServerManagement;
        [Inject] private Barrier              m_Barrier;

        public bool HasLinkToPlayer(Entity entity)
        {
            var gr = this.GetComponentGroup();
            return false;
        }

        public NativeList<Entity> GetPlayerCharacters(Entity entity)
        {
            UpdateInjectedComponentGroups();
            
            var list   = new NativeList<Entity>(Allocator.Temp);
            var length = m_PlayerCharacterGroup.CalculateLength();

            var entities     = m_PlayerCharacterGroup.GetEntityArray();
            var playerOwners = m_PlayerCharacterGroup.GetComponentDataArray<CharacterPlayerOwner>();
            for (int i = 0; i != length; i++)
            {
                if (playerOwners[i].Target == entity)
                    list.Add(entities[i]);
            }

            return list;
        }

        private ComponentGroup m_PlayerCharacterGroup;

        protected override void OnCreateManager()
        {
            m_PlayerCharacterGroup = GetComponentGroup
            (
                typeof(CharacterTag),
                typeof(CharacterPlayerOwner)
            );

            m_MsgIdRegisterSystem.Register(this);
        }

        protected override void OnUpdate()
        {

        }
    }
}