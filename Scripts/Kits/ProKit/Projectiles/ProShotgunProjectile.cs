using System.Linq;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Kits.ProKit.ProShotgun
{
	public struct ProShotgunProjectile : IComponentData
	{
		public struct Create
		{
			public Entity Owner;
			public float3 Position;
			public float3 Velocity;
		}

		[UpdateInGroup(typeof(ProProjectileProcessSystemGroup)), UpdateBefore(typeof(ProProjectileProcessSystem))]
		public class ProcessSystem : GameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.WithAll<ProShotgunProjectile>().ForEach((ref Velocity velocity) =>
				{
					var s = 1.25f;

					velocity.Value = math.lerp(velocity.Value, 0, ServerTick.Delta * s);
				});
			}
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = ProProjectile.ProviderBasicComponents
				                                .Append(typeof(ProShotgunProjectile))
				                                .ToArray();
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.SetComponentData(entity, new Translation {Value                   = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value                      = data.Velocity});
				EntityManager.SetComponentData(entity, new ProProjectile.Settings {detectRadius = 0.1f, damageRadius                      = 0.11f, damage = 4});
				EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase  = StandardProjectilePhase.Active, endTick = ServerTick.AddMs(1000)});
				EntityManager.ReplaceOwnerData(entity, data.Owner);

				EntityManager.AddComponentData(entity, new DebugRenderSphere {CastShadows = true, ReceiveShadows = true, Color = Color.yellow});
				EntityManager.AddComponentData(entity, new Scale {Value                   = 0.2f});
			}
		}
	}
}