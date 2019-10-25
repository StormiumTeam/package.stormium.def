using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Mathematics;
using Unity.Physics;

namespace CharacterController
{
	public partial class CharacterSystem
	{
		private unsafe partial struct UpdateJob
		{
			private bool CanWallBounce(ref MoveData moveData, SrtJumpMovementComponent jumpComponent, SrtDodgeMovementComponent dodgeComponent, SrtWallBounceMovementComponent wbComponent, float3 direction, out ColliderCastHit closestHit)
			{
				var shouldWallBounce = (jumpComponent.JumpQueued >= Tick && wbComponent.EnableWallJump) || (dodgeComponent.DodgeQueued >= Tick && wbComponent.EnableWallDodge);
				if (!shouldWallBounce || UTick.AddMsNextFrame(wbComponent.LastBounceTick, 100) >= Tick)
				{
					closestHit = default;
					return false;
				}

				const float surroundingSensibility = 0.2f;
				const float directionSensibility   = 0.25f;

				var probeGeom = moveData.Probe->Geometry;
				probeGeom.Radius         = moveData.Collider->Radius + surroundingSensibility;
				moveData.Probe->Geometry = probeGeom;

				var castInput = new ColliderCastInput
				{
					Collider    = (Collider*) moveData.Probe,
					Orientation = quaternion.identity,
					Start       = moveData.Position,
					End         = moveData.Position + math.normalizesafe(direction) * directionSensibility
				};
				return PhysicsWorld.CastCollider(castInput, out closestHit) && math.abs(closestHit.SurfaceNormal.y) <= 0.1f;
			}
		}
	}
}