using StormiumShared.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Scripts
{
	public class InterpolateTransformSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			var tick = Tick;

			if (GameMgr.GameType == GameType.Server)
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