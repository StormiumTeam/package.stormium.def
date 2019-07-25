using System.Linq;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Kits.ProKit.ProPulseGun
{
	public struct ProPulseGunProjectile : IComponentData
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
				                                .Append(typeof(ProPulseGunProjectile))
				                                .ToArray();
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.SetComponentData(entity, new Translation {Value = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value    = data.Velocity});
				EntityManager.SetComponentData(entity, new ProProjectile.Settings
				{
					detectRadius = 0.1f,
					damageRadius = 0.11f,
					damage       = 5
				});
				EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase = StandardProjectilePhase.Active});
				EntityManager.ReplaceOwnerData(entity, data.Owner);

				EntityManager.AddComponentData(entity, new DebugRenderSphere {CastShadows = true, ReceiveShadows = true, Color = Color.cyan});
				EntityManager.AddComponentData(entity, new Scale {Value                   = 0.1f});
			}
		}
	}
}