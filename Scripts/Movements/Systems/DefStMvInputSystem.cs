using package.stormium.core;
using package.stormiumteam.networking;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateBefore(typeof(UpdateRigidbodySystem))]
    public class DefStMvRunInputSystem : ComponentSystem
    {
        [Inject] private Group m_Group;

        public MessageIdent MsgReadInputId = new MessageIdent
        {
            Id      = $"{nameof(def)}/{nameof(DefStMvRunInputSystem)}/{nameof(MsgReadInputId)}",
            Version = 0
        };

        public MessageIdent MsgWriteInputId = new MessageIdent
        {
            Id      = $"{nameof(def)}/{nameof(DefStMvRunInputSystem)}/{nameof(MsgWriteInputId)}",
            Version = 0
        };

        protected override void OnCreateManager(int capacity)
        {
            var msgRegister = World.Active.GetOrCreateManager<MsgIdRegisterSystem>();
            MsgWriteInputId = msgRegister.Register(MsgWriteInputId);
            MsgReadInputId  = msgRegister.Register(MsgReadInputId);
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var input = new DefStMvInput();
                input.RunDirection = new float3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

                input.Jump      = Input.GetButtonDown("Jump") ? 1 : 0;
                input.Dodge     = Input.GetAxisRaw("Sprint");
                input.WallJump  = Input.GetButtonDown("Jump") ? 1 : 0;
                input.WallDodge = Input.GetButtonDown("Sprint") ? 1 : 0;

                m_Group.Inputs[i] = input;
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>  Characters;
            public ComponentDataArray<DefStMvInput> Inputs;

            public readonly int Length;
        }
    }
}