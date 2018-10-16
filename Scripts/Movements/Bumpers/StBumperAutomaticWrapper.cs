using System;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct StBumperAutomatic : ISharedComponentData
    {
        public Collider TriggerCollider;
    }

    [RequireComponent(typeof(StBumperPlatformWrapper))]
    public class StBumperAutomaticWrapper : SharedComponentDataWrapper<StBumperAutomatic>
    {
    }
}