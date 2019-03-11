using System;
using System.Collections.Generic;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Scripts
{
	public class StandardGameLoop : IGameLoop
	{
		private World m_World;
		
		public World OnAwake()
		{
			DefaultWorldInitialization.Initialize("Standard GameLoop", false);
			//m_World = new World("StandardGameLoop");
			m_World = World.Active;

			/*// Add External Dependencies
			m_World.GetOrCreateManager<NetworkManager>();
			m_World.GetOrCreateManager<NetworkEventManager>();
			m_World.GetOrCreateManager<NetworkCreateIncomingInstanceSystem>();
			m_World.GetOrCreateManager<NetworkValidateInstances>();
			m_World.GetOrCreateManager<NetPatternSystem>();
			
			// Add Managers
			m_World.AddManager(new EntityManager());
			m_World.AddManager(new StGameTimeManager());
			m_World.AddManager(new PhysicQueryManager());
			m_World.AddManager(new StormiumGameServerManager());
			m_World.AddManager(new StormiumGameManager());
			
			// Snapshot System... (in)
			m_World.AddManager(new NetworkSnapshotManagerGetIncoming());
			m_World.AddManager(new NetworkSnapshotManager());
			
			// Snapshot System... (out)
			m_World.AddManager(new NetworkSnapshotManagerPushIncoming());*/

			return m_World;
		}

		public void Shutdown()
		{
			m_World.Dispose();
		}

		public void OnUpdate()
		{
			
		}

		public void OnFixedUpdate()
		{
			
		}

		public void OnLateUpdate()
		{
			
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void AutoSetLoop()
		{
			/*var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asm in assemblies)
			{
				var types = asm.GetTypes();
				var componentTypes = types.Select(t => t.GetInterfaces().Where(i => i == typeof(IComponentData) || i == typeof(IOwnerDescription)));
			}*/
			
			//GameLoopManager.RequestLoop<StandardGameLoop>();
		}

		private static List<Type> m_DataChangedSystems = new List<Type>();
	}
}