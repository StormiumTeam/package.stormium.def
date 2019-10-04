using System;
using System.Collections.Generic;
using System.Linq;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using ProKit;
using Revolution;
using Revolution.NetCode;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class DeathMatchServer : GameModeAsyncSystem<DeathMatchGameMode>
	{
		protected override void OnCreateMachine(ref Machine machine)
		{
			machine.SetCollection(new BlockAutoLoopCollection("Server Loop", new List<Block>
			{
				new DestroyAllCharacters(),
				new BlockAutoLoopCollection("Loop", new List<Block>
				{
					new ManageCharacterBlock()
				}),
				new DestroyAllCharacters(),
			}));
			machine.AddContext(new QueryContext
			{
				PlayerWithoutCharacter = GetEntityQuery(new EntityQueryDesc
				{
					All  = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag)},
					None = new ComponentType[] {typeof(PlayerCharacter)}
				}),
				PlayerWithCharacter= GetEntityQuery(new EntityQueryDesc
				{
					All  = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag), typeof(PlayerCharacter)},
				})
			});
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
			using (var playerEntities = query.PlayerWithoutCharacter.ToEntityArray(Allocator.TempJob))
			{
				foreach (var player in playerEntities)
				{
					characterProvider.CurrentPlayer = player;

					var character = characterProvider.SpawnLocalEntityWithArguments(new ProKitCreateCharacter
					{

					});

					worldCtx.EntityMgr.AddComponentData(player, new PlayerCharacter {Character = character});
				}
			}

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
				typeof(GhostEntity)
			}).ToArray();
		}

		public override void SetEntityData(Entity entity, ProKitCreateCharacter data)
		{
			base.SetEntityData(entity, data);

			EntityManager.ReplaceOwnerData(entity, CurrentPlayer);
		}
	}

	public class QueryContext : ExternalContextBase
	{
		public EntityQuery PlayerWithoutCharacter;
		public EntityQuery PlayerWithCharacter;
	}
}