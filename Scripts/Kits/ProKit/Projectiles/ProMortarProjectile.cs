using System.Linq;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Stormium.Default.Kits.ProKit.ProMortar
{
	public struct ProMortarProjectile : IComponentData
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
				                                .Append(typeof(ProMortarProjectile))
				                                .ToArray();
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.SetComponentData(entity, new Translation {Value = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value    = data.Velocity});
				EntityManager.SetComponentData(entity, new ProProjectile.Settings
				{
					detectRadius = 0.25f,
					damageRadius = 3.25f,
					damage       = 25,
					gravity      = new float3(0, -27.5f, 0)
				});
				EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase = StandardProjectilePhase.Active});
				EntityManager.ReplaceOwnerData(entity, data.Owner);
			}
		}
	}
}