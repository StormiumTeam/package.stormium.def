using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace RPCs
{
	public struct RequestToSpectateEntityRpc : IRpcCommandRequestComponentData
	{
		public uint GhostTarget;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(GhostTarget);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			GhostTarget = reader.ReadUInt(ref ctx);
		}

		public Entity SourceConnection { get; set; }
	}

	public struct SpectateEntityRequest : IComponentData
	{
		public Entity Spectator;
		public Entity Target;
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	public class ServerProcessSpectateEntityRequest : ComponentSystem
	{
		private EntityQuery m_RpcGroup;
		private EntityQuery m_RequestGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_RpcGroup = GetEntityQuery(typeof(RequestToSpectateEntityRpc), ComponentType.Exclude<SendRpcCommandRequestComponent>());
			m_RequestGroup = GetEntityQuery(typeof(SpectateEntityRequest));
		}

		protected override void OnUpdate()
		{
			// Transform all RPC request
			Entities.With(m_RpcGroup).ForEach((Entity reqEntity, ref RequestToSpectateEntityRpc req) =>
			{
				var requestCopy = req;

				Entity ghostEntity = default;
				Entities.ForEach((Entity entity, ref GhostIdentifier ghostIdentifier) =>
				{
					if (ghostIdentifier.Value == requestCopy.GhostTarget)
					{
						ghostEntity = entity; 
					}
				});

				// we can't directly use a gameplayer as a spectated entity, so get the character of it
				if (EntityManager.HasComponent<GamePlayer>(ghostEntity))
				{
					var children = EntityManager.GetBuffer<OwnerChild>(ghostEntity);
					if (children.Length > 0)
						ghostEntity = children[0].Child;
				}

				var transformed = EntityManager.CreateEntity(typeof(SpectateEntityRequest));
				EntityManager.SetComponentData(transformed, new SpectateEntityRequest
				{
					Target    = ghostEntity,
					Spectator = EntityManager.GetComponentData<CommandTargetComponent>(req.SourceConnection).targetEntity
				});
				EntityManager.DestroyEntity(reqEntity);
			});

			Entities.With(m_RequestGroup).ForEach((Entity ent, ref SpectateEntityRequest request) =>
			{
				Debug.Log($"Request to spectate target: {request.Target}, from {request.Spectator}.");

				var serverCamera = EntityManager.GetComponentData<ServerCameraState>(request.Spectator);
				{
					serverCamera.Data.Target = request.Target;
				}
				EntityManager.SetComponentData(request.Spectator, serverCamera);
				EntityManager.DestroyEntity(ent);
			});
		}
	}
}