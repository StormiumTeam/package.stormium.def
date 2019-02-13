using package.stormium.def;
using package.stormium.def.Kits.ProKit;
using package.stormiumteam.networking;
using Runtime;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Stormium.Default.Tests
{
    [UpdateAfter(typeof(ProKitBehaviorSystem))]
    [DisableAutoCreation]
    public class TestCharacterManager : ComponentSystem
    {
        public Entity chrEntity;
        public BasicUserCommand userCommand;

        protected override void OnStartRunning()
        {
            var modelIdent = World.GetExistingManager<TestCharacterProvider>().GetModelIdent();
            
            chrEntity = World.GetExistingManager<StormiumGameManager>().SpawnLocal(modelIdent);

            EntityManager.AddComponent(chrEntity, typeof(EntityAuthority));
            EntityManager.SetComponentData(chrEntity, new ProKitBehaviorSettings
            {
                GroundSettings = SrtGroundSettings.NewBase(),
                AerialSettings = SrtAerialSettings.NewBase()
            });

            var camera = EntityManager.CreateEntity(typeof(LocalCameraState));
            
            EntityManager.SetComponentData(camera, new LocalCameraState{Target = chrEntity, Mode = CameraMode.Forced});
        }

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
                Cursor.lockState = CursorLockMode.Locked;
            else if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;

            if (chrEntity == default)
                return;
            
            var mv = new float2();
            mv.x = Input.GetAxisRaw("Horizontal");
            mv.y = Input.GetAxisRaw("Vertical");
            
            var input = new ProKitInputState(mv, Input.GetKey(KeyCode.Space), Input.GetKey(KeyCode.LeftShift));

            userCommand.Look = GetNewAimLook(userCommand.Look);
            
            EntityManager.SetComponentData(chrEntity, input);
            EntityManager.SetComponentData(chrEntity, new AimLookState(userCommand.Look));

            /*var chrTr = EntityManager.GetComponentObject<Transform>(chrEntity);
            var cm = Camera.main;

            if (cm != null)
            {
                var cmTr = cm.transform;
                cmTr.position = chrTr.position + new Vector3(0, 1.6f, 0);
                cmTr.rotation = Quaternion.Euler(userCommand.Look.y, userCommand.Look.x, 0);
            }*/
        }

        private float2 GetNewAimLook(float2 previous)
        {
            var input = new float2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 1.5f;
                    
            var newRotation = previous + input;
            newRotation.x = newRotation.x % 360;
            newRotation.y = Mathf.Clamp(newRotation.y, -89f, 89f);

            return newRotation;
        }
    }
}