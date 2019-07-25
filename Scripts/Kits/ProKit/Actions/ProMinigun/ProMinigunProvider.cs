using System;
using System.Collections.Generic;
using package.stormiumteam.networking.runtime.lowlevel;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Stormium.Default.Actions.ProMinigun
{
	public class ProMinigunProjectileProvider : BaseProviderBatch<ProMinigunProjectileProvider.Create>
	{
		public struct Create
		{
			public Entity Owner;
			public float3 Position;
			public float3 Velocity;
		}
		
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<ProjectileDescription>(),
				ComponentType.ReadWrite<ProProjectile.Settings>(),
				ComponentType.ReadWrite<ProProjectile.PredictedState>(),
				ComponentType.ReadWrite<ProMinigunProjectile>(),
				ComponentType.ReadWrite<Translation>(),
				ComponentType.ReadWrite<Rotation>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.ReadWrite<Velocity>()
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new Translation {Value = data.Position});
			EntityManager.SetComponentData(entity, new Velocity(data.Velocity));
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase = StandardProjectilePhase.Active});
		}
	}
}