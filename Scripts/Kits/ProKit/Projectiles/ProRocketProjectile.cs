using System.Linq;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Stormium.Default
{
	public struct ProRocketProjectile : IComponentData
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
				                                .Append(typeof(ProRocketProjectile))
				                                .ToArray();
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.SetComponentData(entity, new Translation {Value                   = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value                      = data.Velocity});
				EntityManager.SetComponentData(entity, new ProProjectile.Settings {detectRadius = 0.1f, damageRadius                      = 0.11f, damage = 4});
				EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase  = StandardProjectilePhase.Active, endTick = ServerTick.AddMs(3000)});
				EntityManager.ReplaceOwnerData(entity, data.Owner);
			}
		}
	}
}