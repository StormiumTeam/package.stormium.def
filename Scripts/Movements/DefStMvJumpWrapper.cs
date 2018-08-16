using System;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    public struct DefStMvJumpExecutable : IComponentData, IExecutableTag
    {
        
    }
    
    [Serializable]
    public struct DefStMvJump : IComponentData
    {
        public float BaseVerticalForce;
        public int MaximumConsecutiveAirJump;
        public float MinTimeBetweenJumps;
        public float MaxTimeBetweenJumps;
        public float GravityComplementForce;
    }

    [Serializable]
    public struct DefStMvJumpState : IComponentData
    {
        public int CurrentCombo;
        public float ActionStartTime;
        public int CurrentComboFromGround;
    }

    public class DefStMvJumpWrapper : BetterComponentWrapper<DefStMvJump>
    {
        public DefStMvJumpWrapper()
        {
            Value = new DefStMvJump
            {
                BaseVerticalForce = 0.275f,
                MaximumConsecutiveAirJump = 1,
                MinTimeBetweenJumps = 0.1f,
                MaxTimeBetweenJumps = 0.75f,
                GravityComplementForce = 1f
            };
        }
    }
}