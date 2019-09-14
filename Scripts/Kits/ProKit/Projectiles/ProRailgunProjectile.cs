using System.Linq;
using StormiumTeam.GameBase;
using Stormium.Default.Kits.ProKit;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Scripts.Actions.ProRailgun
{
	public struct ProRailgunProjectile : IProjectile, IComponentData
	{
		public float  ScanRadius;
		public float3 HitPoint;
		public float3 Direction;

		public struct Create
		{
			public Entity Owner;
			public float3 Position;
			public float3 Direction;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = ProHitScan.ProviderBasicComponents
				                             .Append(typeof(ProRailgunProjectile))
				                             .ToArray();
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.SetComponentData(entity, new Translation {Value                   = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value                      = data.Direction});
				EntityManager.SetComponentData(entity, new ProProjectile.Settings {detectRadius = 0.1f, damageRadius                      = 0.11f, damage = 4});
				EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase  = StandardProjectilePhase.Active, endTick = UTick.AddMsNextFrame(ServerTick, 100)});
				EntityManager.ReplaceOwnerData(entity, data.Owner);
			}
		}
	}
}