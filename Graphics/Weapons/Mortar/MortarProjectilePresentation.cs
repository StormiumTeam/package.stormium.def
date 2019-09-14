using Stormium.Default.Kits.ProKit.ProMortar;
using StormiumTeam.GameBase;
using Unity.Transforms;
using UnityEngine;

namespace Graphics.Weapons
{
	public class MortarProjectilePresentation : RuntimeAssetPresentation<MortarProjectilePresentation>
	{
	}

	public class MortarProjectileBackend : RuntimeAssetBackend<MortarProjectilePresentation>
	{
	}

	public class VisualMortarProjectileSystem : VisualProjectileSystemBase<ProMortarProjectile, MortarProjectilePresentation, MortarProjectileBackend>
	{
		protected override string PresentationAssetId => "Stormium.Default.ProKit.Projectile.Mortar";

		protected override void OnUpdate()
		{
			base.OnUpdate();

			Entities.With(QueryBackend).ForEach((Transform tr, MortarProjectileBackend backend) =>
			{
				if (CheckAndDisableForNextFrame(backend))
					return;

				var localToWorld = backend.DstEntityManager.GetComponentData<LocalToWorld>(backend.DstEntity);
				var velocity     = backend.DstEntityManager.GetComponentData<Velocity>(backend.DstEntity);

				tr.position = localToWorld.Position;
				tr.forward  = velocity.normalized;

				if (backend.Presentation != null)
					backend.Presentation.transform.Rotate(600 * Time.deltaTime, 300f * Time.deltaTime, 25f * Time.deltaTime);
			});
		}
	}
}