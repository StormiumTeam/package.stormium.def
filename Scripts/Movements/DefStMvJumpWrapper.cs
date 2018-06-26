using System;
using package.guerro.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvJump : IComponentData
    {
        public float BaseVerticalForce;
        public int MaximumConsecutiveJump;
        public float MinimalTimeBetweenJumps;
        public float MaxTimeBetweenJumps;
        public float GravityComplementForce;
    }

    [Serializable]
    public struct DefStMvJumpState : IComponentData
    {
        public int CurrentCombo;
        public float ActionStartTime;
    }

    public class DefStMvJumpWrapper : BetterComponentWrapper<DefStMvJump>
    {
        public DefStMvJumpWrapper()
        {
            Value = new DefStMvJump
            {
                BaseVerticalForce = 0.275f,
                MaximumConsecutiveJump = 2,
                MinimalTimeBetweenJumps = 0.1f,
                MaxTimeBetweenJumps = 0.75f,
                GravityComplementForce = 1f
            };
        }
    }
}