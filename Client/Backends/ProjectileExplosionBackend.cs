using System;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Default.Client.Visual
{
	public class ProjectileExplosionBackend : RuntimeAssetBackend<ProjectileExplosionPresentationBase>
	{

	}

	public abstract class ProjectileExplosionPresentationBase : RuntimeAssetPresentation<ProjectileExplosionPresentationBase>
	{

	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class ProjectileExplosionBackendSystem : ComponentSystem
	{
		private Lazy<AssetPool<GameObject>> m_Pool = new Lazy<AssetPool<GameObject>>(() => new AssetPool<GameObject>(pool =>
		{
			var gameObject = new GameObject("ProjectileExplosionBackend", typeof(ProjectileExplosionBackend), typeof(GameObjectEntity));
			return gameObject;
		}));

		public Lazy<AssetPool<GameObject>> Pool => m_Pool;

		protected override void OnUpdate()
		{

		}
	}
}