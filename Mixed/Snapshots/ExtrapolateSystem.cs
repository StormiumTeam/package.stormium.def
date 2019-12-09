using DefaultNamespace;
using Revolution;
using Unity.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase.Snapshots
{
	[UpdateAfter(typeof(PredictedTranslationSnapshot.Synchronize))]
	[UpdateAfter(typeof(Velocity.Synchronize))]
	[UpdateInGroup(typeof(SnapshotWithDelegateSystemGroup))]
	public class ExtrapolateSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithAll<ReplicatedEntity>().ForEach((ref Translation translation, ref Velocity velocity) =>
			{
				translation.Value += (float3) Vector3.ClampMagnitude(velocity.Value, 6) * GetTick(true).Delta;
			});
		}
	}
}