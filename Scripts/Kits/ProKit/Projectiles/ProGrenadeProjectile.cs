using System.Linq;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Kits.ProKit.ProGrenade
{
	public struct ProGrenadeProjectile : IComponentData
	{
		public int Foo;

		public struct Create
		{
			public Entity Owner;
			public float3 Position;
			public float3 Velocity;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = ProProjectile.ProviderBasicComponents
				                                .Append(typeof(ProGrenadeProjectile))
				                                .ToArray();
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.SetComponentData(entity, new Translation {Value = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value    = data.Velocity});
				EntityManager.SetComponentData(entity, new ProProjectile.Settings
				{
					bounciness   = 0.5f,
					detectRadius = 0.25f,
					damageRadius = 2.75f,
					damage       = 25,
					bumpRadius   = 3f,
					gravity      = new float3(0, -14.5f, 0),
					maxBounce    = 3
				});
				EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase = StandardProjectilePhase.Active});
				EntityManager.ReplaceOwnerData(entity, data.Owner);

				EntityManager.AddComponentData(entity, new DebugRenderSphere {CastShadows = true, ReceiveShadows = true, Color = Color.green});
				EntityManager.AddComponentData(entity, new Scale {Value                   = 0.25f});
			}
		}
	}
}