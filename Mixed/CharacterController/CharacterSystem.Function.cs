using package.stormium.def;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Mathematics;

namespace CharacterController
{
	public partial class CharacterSystem
	{
		private partial struct UpdateJob
		{
			private void GroundMove(in CharacterInput input, in SrtGroundMovementComponent groundComponent, ref Velocity velocity, ref Stamina stamina)
			{
				var settings = groundComponent.Settings;
				if (input.Crouch)
				{
					settings.BaseSpeed        *= 0.5f;
					settings.SprintSpeed      =  settings.BaseSpeed * 1.5f;
					settings.SurfaceFriction  =  20f;
					settings.FrictionSpeed    =  settings.BaseSpeed * 1.5f;
					settings.FrictionSpeedMin =  settings.BaseSpeed + 0.1f;
					settings.FrictionSpeedMax =  20f;
					settings.FrictionMax      =  0.75f;
					settings.Acceleration     =  10f;

					// gain a bit more stamina when crouching
					stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * DeltaTime * 0.25f, 0, math.max(stamina.Value, stamina.Max));
				}
				else
				{
					settings.FrictionSpeed = settings.SprintSpeed + 0.1f;
				}

				if (velocity.speed < groundComponent.Settings.BaseSpeed && airTime.Value < -1f)
				{
					// gain a bit more stamina when not running
					stamina.Value = math.clamp(stamina.Value + stamina.GainPerSecond * DeltaTime * 0.75f, 0, math.max(stamina.Value, stamina.Max));
				}

				velocity.Value             = SrtMovement.GroundMove(velocity.Value, input.Move, direction, settings, DeltaTime);
				aerialComponent.AirControl = 1.0f;

				jumpComponent.IsJumpingInChain = false;
			}
		}
	}
}