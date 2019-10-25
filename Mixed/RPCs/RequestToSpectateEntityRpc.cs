using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace RPCs
{
	public struct RequestToSpectateEntityRpc : IRpcCommand
	{
		public uint GhostTarget;

		public void Execute(Entity connection, World world)
		{
			var entity = world.EntityManager.CreateEntity(typeof(SpectateEntityRequest));
			world.EntityManager.SetComponentData(entity, new SpectateEntityRequest
			{
				Connection  = connection,
				GhostTarget = GhostTarget
			});
		}

		public void WriteTo(DataStreamWriter writer)
		{
			writer.Write(GhostTarget);
		}

		public void ReadFrom(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			GhostTarget = reader.ReadUInt(ref ctx);
		}
	}

	public struct SpectateEntityRequest : IComponentData
	{
		public Entity Connection;
		public uint   GhostTarget;
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	public class ServerProcessSpectateEntityRequest : ComponentSystem
	{
		private EntityQuery m_RequestGroup;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_RequestGroup = GetEntityQuery(typeof(SpectateEntityRequest));
			
			RequireForUpdate(m_RequestGroup);
		}

		protected override void OnUpdate()
		{
			Entities.With(m_RequestGroup).ForEach((Entity ent, ref SpectateEntityRequest request) =>
			{
				var requestCopy = request;

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
				
				Debug.Log($"Spectate Target: {ghostEntity}");

				// temporary, get the player through this
				if (EntityManager.HasComponent<CommandTargetComponent>(request.Connection))
				{
					var playerTarget = EntityManager.GetComponentData<CommandTargetComponent>(request.Connection);
					var serverCamera = EntityManager.GetComponentData<ServerCameraState>(playerTarget.targetEntity);
					{
						serverCamera.Data.Target = ghostEntity;
					}
					EntityManager.SetComponentData(playerTarget.targetEntity, serverCamera);
				}

				EntityManager.DestroyEntity(ent);
			});
		}
	}
}