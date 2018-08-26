﻿using System;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStEntityAimInput : IComponentData
    {
        public Quaternion Rotation;
    }

    public class DefStEntityAimInputWrapper : BetterComponentWrapper<DefStEntityAimInput>
    {
        public DefStEntityAimInputWrapper()
        {
            Value = new DefStEntityAimInput
            {
                Rotation = Quaternion.identity,
            };
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, Value.Rotation.eulerAngles.normalized);
            //Gizmos.color = Color.green;
            //Gizmos.DrawRay(Value.Position, new Vector3(0, Value.RotationY, 0));
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, Vector3.up);
        }
    }
}