using package.stormium.def.Kits.ProKit;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StandardAssets.Characters.Physics;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Object = UnityEngine.Object;

namespace Stormium.Default.Tests
{
    [UpdateBefore(typeof(PreUpdate))]
    public class TestCharacterProvider : SystemProvider
    {        
        public struct TestCharacter : IComponentData
        {
        }

        struct SerializeJob : IJob
        {
            #region Variables

            public int ModelId;

            public SnapshotReceiver  Receiver;
            public SnapshotRuntime Runtime;
            public DataBufferWriter  Buffer;

            public ComponentDataChangedFromEntity<ProKitMovementSettings>              KitSettingFromEntity;
            public ComponentDataChangedFromEntity<ProKitMovementState>              MovementStateFromEntity;
            public ComponentDataChangedFromEntity<ProKitInputState>              KitInputFromEntity;
            public ComponentDataChangedFromEntity<AimLookState>              AimLookFromEntity;
            public ComponentDataChangedFromEntity<Velocity>              VelocityFromEntity;
            public ComponentDataChangedFromEntity<TransformState>              TransformFromEntity;

            #endregion
            
            public void Execute()
            {
                for (var i = 0; i != Runtime.Entities.Length; i++)
                {
                    if (Runtime.Entities[i].ModelId != ModelId)
                        continue;

                    var entity   = Runtime.Entities[i].Source;
                    
                    byte mask       = 0;
                    byte maskPos    = 0;
                    var  maskMarker = Buffer.WriteByte(0);

                    if (SerializationHelper.Access(ref mask, ref maskPos, Receiver, KitSettingFromEntity, entity, out var kitSettings))
                    {
                        Buffer.WriteValue(kitSettings);
                    }

                    if (SerializationHelper.Access(ref mask, ref maskPos, Receiver, MovementStateFromEntity, entity, out var movementState))
                    {
                        Buffer.WriteValue((half) movementState.AirControl);
                        Buffer.WriteByte(movementState.ForceUnground);
                        Buffer.WriteDynamicIntWithMask((ulong) movementState.AirTime, (ulong) movementState.WallBounceTick);
                    }

                    if (SerializationHelper.Access(ref mask, ref maskPos, Receiver, KitInputFromEntity, entity, out var kitInputs))
                    {
                        Buffer.WriteValue(kitInputs);
                    }

                    if (SerializationHelper.Access(ref mask, ref maskPos, Receiver, AimLookFromEntity, entity, out var aimLook))
                    {
                        Buffer.WriteValue((half2) aimLook.Aim);
                    }

                    if (SerializationHelper.Access(ref mask, ref maskPos, Receiver, VelocityFromEntity, entity, out var velocity))
                    {
                        Buffer.WriteValue((half3) velocity.Value);
                    }

                    if (SerializationHelper.Access(ref mask, ref maskPos, Receiver, TransformFromEntity, entity, out var transformState))
                    {
                        Buffer.WriteValue((half3) transformState.Position);
                        Buffer.WriteValue((half4) transformState.Rotation.value);
                    }

                    Buffer.WriteByte(mask, maskMarker);
                }
            }
        }

        struct DeserializeJob : IJob
        {
            #region Variables

            public int ModelId;

            public SnapshotSender  Sender;
            public SnapshotRuntime Runtime;
            public UnsafeAllocation<DataBufferReader> BufferReference;

            public ComponentDataFromEntity<ProKitMovementSettings>              KitSettingFromEntity;
            public ComponentDataFromEntity<ProKitMovementState>              MovementStateFromEntity;
            public ComponentDataFromEntity<ProKitInputState>              KitInputFromEntity;
            public ComponentDataFromEntity<AimLookState>              AimLookFromEntity;
            public ComponentDataFromEntity<Velocity>              VelocityFromEntity;
            public ComponentDataFromEntity<TransformState>              TransformFromEntity;

            #endregion

            public void Execute()
            {
                ref var buffer = ref BufferReference.AsRef();
                
                for (var i = 0; i != Runtime.Entities.Length; i++)
                {
                    if (Runtime.Entities[i].ModelId != ModelId)
                        continue;

                    var entity   = Runtime.GetWorldEntityFromGlobal(i);
                    var mask = buffer.ReadValue<byte>();
                    byte maskPos = 0;
                    
                    if (MainBit.GetBitAt(mask, maskPos) == 1)
                    {
                        KitSettingFromEntity[entity] = buffer.ReadValue<ProKitMovementSettings>();
                    }

                    maskPos++;
                    if (MainBit.GetBitAt(mask, maskPos) == 1)
                    {
                        var airControl = buffer.ReadValue<half>();
                        var forceUnground = buffer.ReadValue<byte>();
                        buffer.ReadDynIntegerFromMask(out var unsignedAirTime, out var unsignedWallBounceTick);

                        MovementStateFromEntity[entity] = new ProKitMovementState
                        {
                            AirControl     = airControl,
                            ForceUnground  = forceUnground,
                            AirTime        = (int) unsignedAirTime,
                            WallBounceTick = (long) unsignedWallBounceTick
                        };
                    }

                    maskPos++;
                    if (MainBit.GetBitAt(mask, maskPos) == 1)
                    {
                        KitInputFromEntity[entity] = buffer.ReadValue<ProKitInputState>();
                    }

                    maskPos++;
                    if (MainBit.GetBitAt(mask, maskPos) == 1)
                    {
                        var aimLook = buffer.ReadValue<half2>();
                        
                        AimLookFromEntity[entity] = new AimLookState(aimLook);
                    }

                    maskPos++;
                    if (MainBit.GetBitAt(mask, maskPos) == 1)
                    {
                        var velocity = buffer.ReadValue<half3>();
                        
                        VelocityFromEntity[entity] = new Velocity(velocity);
                    }

                    maskPos++;
                    if (MainBit.GetBitAt(mask, maskPos) == 1)
                    {
                        var position = buffer.ReadValue<half3>();
                        var rotation = buffer.ReadValue<half4>();
                        
                        TransformFromEntity[entity] = new TransformState(position, new quaternion(rotation));
                    }
                }
            }
        }

        public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
        {
            entityComponents = new []
            {
                ComponentType.ReadWrite<LivableDescription>(), 
                ComponentType.ReadWrite<CharacterDescription>(), 
                ComponentType.ReadWrite<TestCharacter>(),
                ComponentType.ReadWrite<CameraModifierData>(), 
                ComponentType.ReadWrite<EyePosition>(), 
                ComponentType.ReadWrite<ModelIdent>(), 
                
                ComponentType.ReadWrite<ProKitMovementSettings>(),
                ComponentType.ReadWrite<DataChanged<ProKitMovementSettings>>(),
                
                ComponentType.ReadWrite<ProKitMovementState>(),
                ComponentType.ReadWrite<DataChanged<ProKitMovementState>>(),
                
                ComponentType.ReadWrite<ProKitInputState>(),
                ComponentType.ReadWrite<DataChanged<ProKitInputState>>(),
                
                ComponentType.ReadWrite<AimLookState>(),
                ComponentType.ReadWrite<DataChanged<AimLookState>>(),
                
                ComponentType.ReadWrite<Velocity>(),
                ComponentType.ReadWrite<DataChanged<Velocity>>(),
                
                ComponentType.ReadWrite<TransformState>(), 
                ComponentType.ReadWrite<DataChanged<TransformState>>(), 
                
                ComponentType.ReadWrite<TransformStateDirection>(),
                ComponentType.ReadWrite<SubModel>(), 
                ComponentType.ReadWrite<GenerateEntitySnapshot>()
            };
            excludedComponents = null;
        }

        public override void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
        {
            new SerializeJob
            {
                ModelId = GetModelIdent().Id,

                Receiver = receiver,
                Runtime  = snapshotRuntime,
                Buffer   = data,

                KitSettingFromEntity    = new ComponentDataChangedFromEntity<ProKitMovementSettings>(this),
                MovementStateFromEntity = new ComponentDataChangedFromEntity<ProKitMovementState>(this),
                KitInputFromEntity      = new ComponentDataChangedFromEntity<ProKitInputState>(this),
                AimLookFromEntity       = new ComponentDataChangedFromEntity<AimLookState>(this),
                VelocityFromEntity      = new ComponentDataChangedFromEntity<Velocity>(this),
                TransformFromEntity     = new ComponentDataChangedFromEntity<TransformState>(this),
            }.Run();
        }

        public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
        {
            new DeserializeJob
            {
                ModelId = GetModelIdent().Id,

                Sender          = sender,
                Runtime         = snapshotRuntime,
                BufferReference = UnsafeAllocation.From(ref data),

                KitSettingFromEntity    = GetComponentDataFromEntity<ProKitMovementSettings>(),
                MovementStateFromEntity = GetComponentDataFromEntity<ProKitMovementState>(),
                KitInputFromEntity      = GetComponentDataFromEntity<ProKitInputState>(),
                AimLookFromEntity       = GetComponentDataFromEntity<AimLookState>(),
                VelocityFromEntity      = GetComponentDataFromEntity<Velocity>(),
                TransformFromEntity     = GetComponentDataFromEntity<TransformState>()
            }.Run();
        }

        protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
        {
            var gameObject = new GameObject("ToSet", typeof(Rigidbody), typeof(CapsuleCollider), typeof(OpenCharacterController));
            var goe = gameObject.AddComponent<GameObjectEntity>();

            foreach (var component in EntityComponents)
                EntityManager.AddComponent(goe.Entity, component);

            var loadModelBehavior = gameObject.AddComponent<LoadModelFromStringBehaviour>();

            loadModelBehavior.OnLoadSetSubModelFor(EntityManager, goe.Entity);
            loadModelBehavior.SpawnRoot = gameObject.transform;
            loadModelBehavior.AssetId = "TestCharacter";

            var controller = gameObject.GetComponent<OpenCharacterController>();

            controller.SetLayerMask(GameBaseConstants.CollisionMask);
            controller.SetCenter(new Vector3(0, 1, 0), true, true);

            gameObject.AddComponent<DestroyGameObjectOnEntityDestroyed>();

            var shape = gameObject.AddComponent<CollisionForCharacter>();
            shape.collide = false; // don't collide with other characters

            var cf = snapshotRuntime.Header.Sender.Flags;
            EntityManager.SetComponentData(goe.Entity, cf == SnapshotFlags.Local ? new TransformStateDirection(Dir.ConvertToState) : new TransformStateDirection(Dir.ConvertFromState));
            EntityManager.SetComponentData(goe.Entity, new EyePosition(new float3(0.0f, 1.6f, 0.0f)));
            
            gameObject.name = $"TestCharacter(o={origin}, s={goe.Entity})";
            
            return goe.Entity;
        }

        protected override void DestroyEntity(Entity worldEntity)
        {
            var gameObject = EntityManager.GetComponentObject<Transform>(worldEntity).gameObject;
            
            Object.Destroy(gameObject);
        }
    }
}