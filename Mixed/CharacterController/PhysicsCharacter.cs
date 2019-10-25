using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace CharacterController
{
	public unsafe struct PhysicsCharacter : IComponentData
	{
		public float MaxStepHeight;

		public static float3 GetBottomPosition(in MoveData moveData)
		{
			float3 capsuleBottom;
			{
				var capsuleHeight = moveData.Collider->Vertex1.y - moveData.Collider->Vertex0.y;
				var center        = float3.zero;
				capsuleBottom = center + (-math.up() * (capsuleHeight * 0.5f));
			}

			return moveData.Position + math.mul(moveData.Rotation, capsuleBottom);
		}

		public static GroundResult CheckGround<TAgainst>(in MoveData moveData, in TAgainst collideAgainst, bool debug = true)
			where TAgainst : struct, ICollidable
		{
			var geom = moveData.Probe->Geometry;
			geom.Radius              = moveData.Collider->Radius + 0.08f; // -skinwidth
			moveData.Probe->Geometry = geom;

			var bottomPosition = GetBottomPosition(moveData);
			var input          = new ColliderCastInput();
			input.Collider    =  (Collider*) moveData.Probe;
			input.Orientation =  quaternion.identity;
			input.Start       =  bottomPosition + new float3(0.0f, moveData.Probe->Radius, 0.0f);
			input.End         =  input.Start - new float3(0, 0.08f, 0);
			input.Start       += new float3(0, 0.1f, 0);

			var rad = moveData.Probe->Radius;
			Debug.DrawLine(input.Start - new float3(-rad, 0, 0.0f), input.End - new float3(-rad, rad, 0.0f), Color.magenta); // left
			Debug.DrawLine(input.Start - new float3(rad, 0, 0.0f), input.End - new float3(rad, rad, 0.0f), Color.magenta);   // right
			Debug.DrawLine(input.Start - new float3(0.0f, 0, -rad), input.End - new float3(0.0f, rad, -rad), Color.magenta);
			Debug.DrawLine(input.Start - new float3(0.0f, 0, rad), input.End - new float3(0.0f, rad, rad), Color.magenta);
			Debug.DrawLine(input.Start - new float3(0.0f, 0, 0.0f), input.End - new float3(0.0f, rad, 0.0f), Color.magenta);

			GroundResult groundResult;
			groundResult.State          = GroundState.None;
			groundResult.RigidBodyIndex = -1;

			var allHits = new NativeList<ColliderCastHit>(16, Allocator.Temp);
			collideAgainst.CastCollider(input, ref allHits);
			for (var i = 0; i != allHits.Length; i++)
			{
				groundResult.RigidBodyIndex = allHits[i].RigidBodyIndex;

				if (allHits[i].SurfaceNormal.y > 0.25f)
				{
					if (debug) Debug.DrawLine(input.Start, allHits[i].Position, Color.cyan, 1);
					groundResult.State = GroundState.StableOnGround;
					return groundResult;
				}

				if (allHits[i].SurfaceNormal.y > 0.0f)
				{
					groundResult.State = GroundState.SlideOnGround;
					if (debug) Debug.DrawLine(input.Start, allHits[i].Position, Color.Lerp(Color.yellow, Color.black, 0.25f), 1);
				}
				else
				{
					if (debug) Debug.DrawLine(input.Start, allHits[i].Position, Color.red, 1);
				}
			}

			return groundResult;
		}

		public static void Depenetrate<TAgainst>(ref MoveData moveData, in TAgainst collideAgainst, in int maxIteration = 4)
			where TAgainst : struct, ICollidable
		{
			var previousGeometry = moveData.Collider->Geometry;
			var geom             = previousGeometry;
			geom.Radius                 += 0.01f; // skinwidth
			moveData.Collider->Geometry =  geom;

			var input = new ColliderDistanceInput
			{
				Collider    = (Collider*) moveData.Collider,
				MaxDistance = 0.0f,
				Transform   = new RigidTransform(moveData.Rotation, moveData.Position)
			};

			var depIter   = 0;
			var collector = new ClosestHitCollector<DistanceHit>(0.0f);
			while (depIter < maxIteration && collideAgainst.CalculateDistance(input, ref collector))
			{
				var closestHit = collector.ClosestHit;
				moveData.Position   -= closestHit.SurfaceNormal * (closestHit.Distance - 0.001f);
				moveData.Velocity   -= closestHit.SurfaceNormal * (closestHit.Distance - 0.001f);
				input.Transform.pos =  moveData.Position;

				depIter++;
			}

			moveData.Collider->Geometry = previousGeometry;
		}

		public static MoveResult Move<TAgainst>(MoveData moveData, TAgainst collideAgainst, NativeList<MoveEvent> events = default)
			where TAgainst : struct, ICollidable
		{
			var originalMoveData = moveData;

			// depenetrate first
			Depenetrate(ref moveData, in collideAgainst);

			const int maxIteration = 8;
			for (var iter = 0; iter < maxIteration; iter++)
			{
				bool found = false;

				// check for obstacles first
				{
					var input = new ColliderCastInput
					{
						Collider    = (Collider*) moveData.Collider,
						Orientation = moveData.Rotation,
						Start       = moveData.Position,
						End         = moveData.Position + moveData.Velocity
					};
					var direction = math.normalizesafe(input.End - input.Start);
					var distance  = math.distance(input.Start, input.End);

					var collector = new ClosestHitCollector<ColliderCastHit>(1.0f);
					if (collideAgainst.CastCollider(input, ref collector))
					{
						var hit = collector.ClosestHit;
						var eventType = MoveEventType.Obstacle;
						if (hit.SurfaceNormal.y > 0.25f)
						{
							var project = Vector3.Cross(moveData.Velocity, hit.SurfaceNormal);
							project = Vector3.Cross(hit.SurfaceNormal, project);

							moveData.Velocity += moveData.Velocity * project;

							eventType = MoveEventType.Slide;
						}

						var startVelocity = moveData.Velocity;
						Depenetrate(ref moveData, collideAgainst);

						if (events.IsCreated)
						{
							events.Add(new MoveEvent
							{
								StartPosition = input.Start,
								EndPosition   = moveData.Position,
								SurfaceNormal = hit.SurfaceNormal,
								StartVelocity = startVelocity,
								EndVelocity   = moveData.Velocity,
								Type          = eventType
							});
						}
					}
				}

				moveData.Position += moveData.Velocity;

				break;
				if (!found)
					break;
			}

			Depenetrate(ref moveData, in collideAgainst);

			var groundStatus = CheckGround(moveData, collideAgainst);
			return new MoveResult
			{
				NewPosition  = moveData.Position,
				NewVelocity  = moveData.Velocity,
				GroundStatus = groundStatus
			};
		}

		// This method is not actually used in the Move() function, it is something that you must call
		public static bool StickOnGround<TAgainst>(ref MoveData moveData, in TAgainst collideAgainst, float3 gravity)
			where TAgainst : struct, ICollidable
		{
			var prevPos        = moveData.Position;
			var bottomPosition = GetBottomPosition(in moveData);
			var downCastInput = new ColliderCastInput
			{
				Collider    = (Collider*) moveData.Collider,
				Orientation = moveData.Rotation,
				Start       = moveData.Position,
				End         = moveData.Position - new float3(0, moveData.Character.MaxStepHeight, 0.0f) + gravity
			};

			var allHits = new NativeList<ColliderCastHit>(Allocator.Temp);
			collideAgainst.CastCollider(downCastInput, ref allHits);

			var distance        = math.distance(downCastInput.Start.y, downCastInput.End.y);
			var nearestDistance = distance;
			var nearestIndex    = -1;
			for (var i = 0; i != allHits.Length; i++)
			{
				var dist = allHits[i].Fraction * distance;
				if (dist < nearestDistance && allHits[i].Position.y < bottomPosition.y)
				{
					nearestDistance = dist;
					nearestIndex    = i;
				}
			}

			if (nearestIndex >= 0)
			{
				moveData.Position.y -= nearestDistance - 0.08f;

				Depenetrate(ref moveData, collideAgainst);
				if ((CheckGround(in moveData, in collideAgainst, false).State & GroundState.TouchGround) != 0)
				{
					moveData.Position = moveData.Position;
					return true;
				}

				moveData.Position = prevPos;
			}

			return false;
		}
	}

	public enum MoveEventType
	{
		Unknown,
		Slide,
		Obstacle
	}

	public struct MoveEvent
	{
		public MoveEventType Type;
		
		public float3 StartPosition;
		public float3 EndPosition;

		public float3 SurfaceNormal;
		public float3 StartVelocity;
		public float3 EndVelocity;
	}

	public unsafe struct MoveData
	{
		public PhysicsCharacter Character;
		
		public float3     Position;
		public quaternion Rotation;
		public float3     Velocity;

		public SphereCollider*  Probe;
		public CapsuleCollider* Collider;
	}

	public struct MoveResult
	{
		public float3       NewPosition;
		public float3       NewVelocity;
		public GroundResult GroundStatus;
	}

	public struct GroundResult
	{
		public GroundState State;
		public int RigidBodyIndex;
	}

	[Flags]
	public enum GroundState
	{
		None = 0,

		/// <summary>
		/// Is the character touching the ground, no matter if he is stable or not? (This value can't be returned as is)
		/// </summary>
		TouchGround = 1,
		StableOnGround = 3,
		SlideOnGround  = 5
	}
}