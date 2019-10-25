using System;
using System.Collections.Generic;
using System.Linq;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using ProKit;
using Revolution;
using Revolution.NetCode;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DefaultNamespace
{
	/*[UpdateInGroup(typeof(OrderGroup.PreFrame.Rules))]
	public class DeathMatchRules : RuleBaseSystem
	{
		public RuleProperties<DeathMatchGameMode>.Property<int> TimeLimit;

		protected override void OnCreate()
		{
			base.OnCreate();

			var rule = AddRule(out DeathMatchGameMode ruleData);
			{
				TimeLimit = rule.Add("End Time", ref ruleData, ref ruleData.TimeLimit);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}
	}*/

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.GameMode))]
	[UpdateInWorld(WorldType.ServerWorld)]
	public class DeathMatchServer : GameModeAsyncSystem<DeathMatchGameMode>
	{
		protected override void OnCreateMachine(ref Machine machine)
		{
			World.GetOrCreateSystem<DeathMatchEventSystem>();
			
			machine.AddContext(new QueryContext
			{
				PlayerWithoutCharacter = GetEntityQuery(new EntityQueryDesc
				{
					All  = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag)},
					None = new ComponentType[] {typeof(PlayerCharacter)}
				}),
				PlayerWithCharacter = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag), typeof(PlayerCharacter)},
				})
			});
			machine.AddContext(new StandardTickGetter
			{
				Group = World.GetOrCreateSystem<ServerSimulationSystemGroup>()
			});

			var endTimeVariable = new Variable<uint>();
			machine.SetCollection(new BlockAutoLoopCollection("Server Loop", new List<Block>
			{
				new DestroyAllCharacters(),
				new BlockAutoLoopCollection("RoundLoop", new List<Block>
				{
					new WarmUpBlock(),
					new Block("SkipFrame"),
					new BlockAutoLoopCollection("GameLoop", new List<Block>
					{
						new ManageCharacterBlock(),
						new EventBlock()
					})
				}),
				new DestroyAllCharacters(),
			}));
		}

		protected override void OnLoop(Entity gameModeEntity)
		{
			Machine.Update();
		}
	}

	public class DestroyAllCharacters : Block
	{
		public DestroyAllCharacters()
		{
			Name = "Destroy All Characters";
		}

		protected override bool OnRun()
		{
			var worldCtx = Context.GetExternal<WorldContext>();
			var query    = Context.GetExternal<QueryContext>();
			if (query == null)
				throw new InvalidOperationException("Context not added!");

			using (var playerCharacterArray = query.PlayerWithCharacter.ToComponentDataArray<PlayerCharacter>(Allocator.TempJob))
			{
				foreach (var charEntity in playerCharacterArray)
				{
					if (worldCtx.EntityMgr.Exists(charEntity.Character))
						worldCtx.EntityMgr.DestroyEntity(charEntity.Character);
				}
			}

			worldCtx.EntityMgr.RemoveComponent(query.PlayerWithCharacter, typeof(PlayerCharacter));

			return true;
		}
	}

	public class WarmUpBlock : Block
	{
		public WarmUpBlock()
		{
			Name = "WarmUp";
		}

		protected override bool OnRun()
		{
			return true;
		}
	}

	public class ManageCharacterBlock : Block
	{
		public ManageCharacterBlock()
		{
			Name = "Manage Characters";
		}

		protected override bool OnRun()
		{
			var worldCtx = Context.GetExternal<WorldContext>();
			var query    = Context.GetExternal<QueryContext>();
			if (query == null)
				throw new InvalidOperationException("Context not added!");

			var characterProvider = worldCtx.GetOrCreateSystem<DeathMatchCharacterProvider>();
			var defHealthProvider = worldCtx.GetOrCreateSystem<DefaultHealthData.InstanceProvider>();
			using (var playerEntities = query.PlayerWithoutCharacter.ToEntityArray(Allocator.TempJob))
			{
				foreach (var player in playerEntities)
				{
					characterProvider.CurrentPlayer = player;

					var character = characterProvider.SpawnLocalEntityWithArguments(new ProKitCreateCharacter
					{

					});

					worldCtx.EntityMgr.AddComponentData(player, new PlayerCharacter {Character = character});
					worldCtx.EntityMgr.SetComponentData(player, new ServerCameraState {Data    = new CameraState {Target = character}});

					defHealthProvider.SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
					{
						owner = character,
						value = 0,
						max   = 100
					});
				}
			}

			return true;
		}
	}

	public class EventBlock : Block
	{
		public EventBlock()
		{
			Name = "Events";
		}

		protected override bool OnRun()
		{
			return true;
		}
	}

	public class DeathMatchCharacterProvider : ProKitCharacterProvider
	{
		public Entity CurrentPlayer;

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			base.GetComponents(out entityComponents);

			entityComponents = entityComponents.Concat(new ComponentType[]
			{
				typeof(Relative<PlayerDescription>),
				typeof(Owner),
				typeof(GhostEntity),
				typeof(TranslationSnapshot.Exclude),
				typeof(PredictedTranslationSnapshot.Use)
			}).ToArray();
		}

		public override void SetEntityData(Entity entity, ProKitCreateCharacter data)
		{
			base.SetEntityData(entity, data);

			EntityManager.ReplaceOwnerData(entity, CurrentPlayer);
			EntityManager.SetComponentData(entity, new Relative<PlayerDescription>(CurrentPlayer));
		}
	}
	
	[UpdateInGroup(typeof(OrderGroup.Simulation.ProcessEvents))]
	public class DeathMatchEventSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((ref TargetImpulseEvent impulseEvent) =>
			{
				if (EntityManager.HasComponent<Velocity>(impulseEvent.Destination))
				{
					var velocity = EntityManager.GetComponentData<Velocity>(impulseEvent.Destination);
					velocity.Value *= impulseEvent.Momentum;
					velocity.Value += impulseEvent.Force;
					
					EntityManager.SetComponentData(impulseEvent.Destination, velocity);
				}
			});
		}
	}

	public class QueryContext : ExternalContextBase
	{
		public EntityQuery PlayerWithoutCharacter;
		public EntityQuery PlayerWithCharacter;
	}

	public class StandardTickGetter : ExternalContextBase, ITickGetter
	{
		public ServerSimulationSystemGroup Group;
		
		public UTick GetTick()
		{
			return Group.GetTick();
		}
	}
}