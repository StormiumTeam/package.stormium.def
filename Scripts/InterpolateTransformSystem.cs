using Runtime;
using Stormium.Core;
using Stormium.Default.States;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Scripts
{
	public class InterpolateTransformSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			var gameMgr = World.GetExistingManager<StormiumGameManager>();
			var tick = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton().Tick;

			if (gameMgr.GameType == GameType.Server)
				return;
			
			ForEach((DynamicBuffer<InterpolationBuffer> buffer, ref InterpolationData interpolationData, ref TransformState transform) =>
			{
				if (buffer.Length <= 3)
				{
					Debug.Log("happens...");
					
					transform.Position = buffer[0].Position;
					return;
				}

				var first = buffer[0];
				var gameTime = EntityManager.GetComponentData<GameTimeComponent>(interpolationData.Instance).Value;

				/*if (interpolationData.Lock1 != 0 && interpolationData.Lock2 != 0)
				{
					
				}*/

				for (var i = 0; i != buffer.Length; i++)
				{
					var item = buffer[i];
					
					// Way too far
					if (gameTime.Tick > item.Tick + gameTime.DeltaTick * 4)
					{
						buffer.RemoveAt(i--);
						continue;
					}
				}

				var last = buffer[buffer.Length - 1];
				var t = (float)(gameTime.Tick - first.Tick) / (last.Tick - first.Tick);

				transform.Position = Vector3.Lerp(first.Position, last.Position, t);
			});
		}
	}
}