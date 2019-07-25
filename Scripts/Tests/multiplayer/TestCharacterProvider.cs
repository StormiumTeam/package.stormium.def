using package.stormium.def.Kits.ProKit;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StandardAssets.Characters.Physics;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Stormium.Default.Tests
{
    public class TestCharacterProvider : BaseProvider
    {        
        public struct TestCharacter : IComponentData
        {
        }

        public override void GetComponents(out ComponentType[] entityComponents)
        {
            entityComponents = new []
            {
                ComponentType.ReadWrite<LivableDescription>(), 
                ComponentType.ReadWrite<CharacterDescription>(), 
                ComponentType.ReadWrite<TestCharacter>(),
                ComponentType.ReadWrite<CameraModifierData>(), 
                ComponentType.ReadWrite<EyePosition>(), 
                ComponentType.ReadWrite<ModelIdent>(), 
                ComponentType.ReadWrite<LivableHealth>(), 
                ComponentType.ReadWrite<HealthContainer>(), 
                
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
        }
    }
}