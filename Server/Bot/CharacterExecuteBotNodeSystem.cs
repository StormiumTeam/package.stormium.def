using CharacterController;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace.Bot
{
	public class CharacterExecuteBotNodeSystem : GameBaseSystem
	{
		private struct CurrentNode : IComponentData
		{
			public int   Index;
			public float Time;

			public float3 previousDirection;
		}

		private static float s_Delta;

		protected override void OnUpdate()
		{
			Entities.WithNone<CurrentNode>().WithAll<BotNode>().ForEach((ent) =>
			{
				EntityManager.AddComponentData(ent, new CurrentNode
				{
					Index = -1
				});
			});

			s_Delta = GetTick(true).Delta;
			Entities.ForEach((DynamicBuffer<BotNode> nodes, ref CurrentNode current, ref Translation translation, ref CharacterInput input) =>
			{
				// first time
				if (current.Index < 0)
				{
					// get nearest node
					var nearest  = -1;
					var distance = float.MaxValue;
					for (var i = 0; i != nodes.Length; i++)
					{
						if (math.distance(nodes[i].Value, translation.Value) >= distance)
							continue;

						distance = math.distance(nodes[i].Value, translation.Value);
						nearest  = i;
					}

					current.Index = nearest;
				}

				// no node found...
				if (current.Index < 0)
					return;

				var node = nodes[current.Index].Value;
				node.y = 0;

				var position = translation.Value;
				position.y = 0;

				var direction = math.normalizesafe(node - position);

				current.previousDirection = math.lerp(current.previousDirection, direction, s_Delta * 1f);
				current.previousDirection = Vector3.MoveTowards(current.previousDirection, direction, s_Delta * 2f);

				direction = current.previousDirection;
				
				var newLook = new float2(((Quaternion) quaternion.LookRotationSafe(direction, math.up())).eulerAngles.y, 0);
				input.Move = new float2(0, 1);
				input.Look = newLook;

				Debug.DrawRay(translation.Value, direction, Color.red, 0.033f);
				Debug.DrawRay(node, math.normalizesafe(position - node), Color.green, 0.033f);

				if (math.distance(node, position) < 1f)
				{
					current.Index++;
					if (current.Index >= nodes.Length)
					{
						current.Index = 0;
					}
					
					// we are at our destination, so let's stop
					if (nodes.Length == 1)
						input.Move = float2.zero;
				}
			});
		}
	}
}