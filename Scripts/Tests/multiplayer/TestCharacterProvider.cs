using package.stormium.def.Kits.ProKit;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;

namespace Stormium.Default.Tests
{
    public class TestCharacterProvider : BaseProvider
    {
        public struct TestCharacter : IComponentData
        {
        }

        public override void GetComponents(out ComponentType[] entityComponents)
        {
            entityComponents = new[]
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
                ComponentType.ReadWrite<ProKitMovementState>(),
                ComponentType.ReadWrite<ProKitInputState>(),

                ComponentType.ReadWrite<AimLookState>(),
                ComponentType.ReadWrite<Velocity>(),
                ComponentType.ReadWrite<SubModel>()
            };
        }
    }
}