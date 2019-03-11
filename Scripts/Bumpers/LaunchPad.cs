using System;
using System.Collections.Generic;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Scripts.Bumpers
{
	[Serializable]
	public struct LaunchPad : IComponentData
	{
		public float3 direction;
		public float3 reset;
		public float force;
	}

	public class LaunchPadSystem : GameBaseSystem
	{
		private struct ValuePadCooldown
		{
			public Entity PadEntity;
			public int LastTick;
		}
		
		private struct Static_ForEachData
		{
			public Collider   PadCollider;
			public Vector3    PadPosition;
			public Quaternion PadRotation;
			public LaunchPad  Pad;
			public Entity     PadEntity;
			public int Tick;

			public EntityCommandBuffer PostUpdateCommands;

			// TODO: Replace this with a buffer on the pad entity instead
			public Dictionary<Entity, ValuePadCooldown> Cooldowns;
		}

		private static Static_ForEachData s_ForEachData;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			s_ForEachData.Cooldowns = new Dictionary<Entity, ValuePadCooldown>();
		}

		protected override void OnUpdate()
		{
			s_ForEachData.Tick = Tick;
			
			ForEach((Entity entity, Transform transform, ref LaunchPad launchPad) =>
			{
				Debug.Assert(transform.GetComponent<Collider>());

				var collider = transform.GetComponent<Collider>();

				s_ForEachData.Pad                = launchPad;
				s_ForEachData.PadEntity          = entity;
				s_ForEachData.PadCollider        = collider;
				s_ForEachData.PadPosition        = transform.position;
				s_ForEachData.PadRotation        = transform.rotation;
				s_ForEachData.PostUpdateCommands = PostUpdateCommands;

				Entities.WithAll<Transform, LivableDescription>().ForEach((Entity otherEntity, Transform otherTransform) =>
				{
					if (!otherTransform.GetComponent<Collider>())
						return;

					var otherCollider = otherTransform.GetComponent<Collider>();
					if (!Physics.ComputePenetration(s_ForEachData.PadCollider, s_ForEachData.PadPosition, s_ForEachData.PadRotation,
						otherCollider, otherTransform.position, otherTransform.rotation,
						out _, out _))
						return;

					// Create Bump event
					Debug.Log("Bump!");

					var canBump = !s_ForEachData.Cooldowns.TryGetValue(otherEntity, out var cooldown) // if the value don't exist in the dictionary, it's ok
					              || cooldown.LastTick + 100 < s_ForEachData.Tick // if the cooldown is over, it's ok
					              || cooldown.PadEntity != s_ForEachData.PadEntity; // if the last triggered pad isn't the same, it's ok

					if (!canBump)
						return;

					s_ForEachData.Cooldowns[otherEntity] = new ValuePadCooldown
					{
						PadEntity = s_ForEachData.PadEntity,
						LastTick  = s_ForEachData.Tick
					};

					var provider = World.Active.GetExistingManager<LaunchPadBumpEventProvider>();
					var delayed  = provider.SpawnLocalEntityDelayed(s_ForEachData.PostUpdateCommands);

					s_ForEachData.PostUpdateCommands.SetComponent(delayed, new TargetBumpEvent
					{
						Direction     = s_ForEachData.Pad.direction,
						VelocityReset = s_ForEachData.Pad.reset,
						Force         = s_ForEachData.Pad.force,
						Position      = s_ForEachData.PadPosition,

						Shooter = s_ForEachData.PadEntity,
						Victim  = otherEntity
					});
				});
			});
		}
	}
}