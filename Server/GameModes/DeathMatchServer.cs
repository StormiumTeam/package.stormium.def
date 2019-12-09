using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Bot;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormium.def;
using package.stormiumteam.shared.ecs;
using ProKit;
using Revolution;
using Unity.NetCode;
using Stormium.Core.Data;
using Stormium.Default;
using Stormium.Default.Mixed;
using Stormium.Default.Mixed.GameModes;
using Stormium.Default.Mixed.Weapons;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.GameMode))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
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
				}),
				Character = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(DeathMatchCharacter), typeof(CharacterDescription), typeof(LivableHealth)}
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
					new CreateBotBlock(new [] {new float3(-22, 0, -10), new float3(-22, 0, 0)}, 4),
					new CreateBotBlock(new [] {new float3(-22, 0, 2)}, 4),
					new CreateBotBlock(new [] {new float3(-22, 0, 4)}, 4),
					new CreateBotBlock(new [] {new float3(-6, 0, 0), new float3(0, 0, 3), new float3(6, 0, 0)}),
					new CreateBotBlock(new [] {new float3(0, 0, 5), new float3(-5, 0, 0), new float3(0, 0, -5), new float3(5, 0, 0)}),
					new CreateBotBlock(new [] {new float3(23, 0, -23), new float3(23.5f, 0, 10), new float3(22, 0, 23), new float3(-4, 0, 23), new float3(-3, 0, 5), new float3(12, 0, -21),  }),
					new Block("SkipFrame"),
					new BlockAutoLoopCollection("GameLoop", new List<Block>
					{
						new ManageCharacterBlock(),
						new RespawnCharacterBlock(),
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

	public class CreateBotBlock : Block
	{
		private float3[] m_Points;
		private float    m_Speed;

		public CreateBotBlock(float3[] points, float speed = float.NaN)
		{
			Name     = "Create Bots";
			m_Points = points;
			m_Speed  = speed;
		}

		protected override bool OnRun()
		{
			//return true;
			
			var worldCtx = Context.GetExternal<WorldContext>();
			var query    = Context.GetExternal<QueryContext>();
			if (query == null)
				throw new InvalidOperationException("Context not added!");

			var characterProvider = worldCtx.GetOrCreateSystem<DeathMatchBotCharacterProvider>();
			var defHealthProvider = worldCtx.GetOrCreateSystem<DefaultHealthData.InstanceProvider>();
			
			characterProvider.Points = new NativeArray<float3>(m_Points, Allocator.Persistent);
			var character = characterProvider.SpawnLocalEntityWithArguments(new ProKitCreateCharacter
			{

			});
			var start = m_Points[0];
			worldCtx.EntityMgr.SetComponentData(character, new Translation {Value        = start});
			worldCtx.EntityMgr.AddComponentData(character, new ForceSpawnPosition {Value = start});

			// ---------------- ---------------- ---------------- //
			// :: Add Health
			defHealthProvider.SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
			{
				owner = character,
				value = 0,
				max   = 100
			});
			
			if (!float.IsNaN(m_Speed))
			{
				Debug.Log($"Spawn Bot with speed={m_Speed}");

				var ground = worldCtx.EntityMgr.GetComponentData<StandardGroundMovement>(character);
				ground.Settings.BaseSpeed   = m_Speed;
				ground.Settings.SprintSpeed = m_Speed;
				worldCtx.EntityMgr.SetComponentData(character, ground);
			}

			characterProvider.Points.Dispose();

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
			var entMgr = worldCtx.EntityMgr;
			var query    = Context.GetExternal<QueryContext>();
			if (query == null)
				throw new InvalidOperationException("Context not added!");

			var teamProvider = worldCtx.GetOrCreateSystem<DeathMatchTeamProvider>();
			var characterProvider = worldCtx.GetOrCreateSystem<DeathMatchCharacterProvider>();
			var defHealthProvider = worldCtx.GetOrCreateSystem<RegenerativeHealthData.InstanceProvider>();
			using (var playerEntities = query.PlayerWithoutCharacter.ToEntityArray(Allocator.TempJob))
			{
				foreach (var player in playerEntities)
				{
					characterProvider.CurrentPlayer = player;
					teamProvider.CurrentPlayer      = player;

					var team = teamProvider.SpawnLocalEntityWithArguments(new DeathMatchTeamProvider.Create
					{

					});
					var character = characterProvider.SpawnLocalEntityWithArguments(new ProKitCreateCharacter
					{

					});

					entMgr.SetComponentData(character, new Translation {Value = new float3(0, 1, 0)});
					entMgr.AddComponentData(character, new WeaponCycle {Index = 0});

					entMgr.SetOrAddComponentData(player, new PlayerCharacter {Character = character});
					entMgr.SetComponentData(player, new ServerCameraState {Data         = new CameraState {Target = character}});

					// ---------------- ---------------- ---------------- //
					// :: Add Health
					var healthEnt = defHealthProvider.SpawnLocalEntityWithArguments(new RegenerativeHealthData.CreateInstance
					{
						owner = character,
						value = 0,
						max   = 100,
						
						cooldown = 2000,
						rate = 5f
					});
					worldCtx.EntityMgr.AddComponent(healthEnt, typeof(GhostEntity));

					// ---------------- ---------------- ---------------- //
					// :: Add Weapons
					{
						// add rocket weapon
						worldCtx.GetOrCreateSystem<ProRocketWeaponProvider>()
						        .SpawnLocalEntityWithArguments(new ProRocketWeaponCreate
						        {
							        Owner  = character,
							        Player = player
						        });
						worldCtx.GetOrCreateSystem<ProRailgunWeaponProvider>()
						        .SpawnLocalEntityWithArguments(new ProRailgunWeaponCreate
						        {
							        Owner  = character,
							        Player = player,
							        Component = new ProRailgunWeaponComponent
							        {
								        RailgunCooldown = 500,
								        RailgunUsage    = 50,
								        BeamCooldown    = 50
							        }
						        });
					}

					entMgr.SetComponentData(character, new LivableHealth {IsDead = true});
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

	public unsafe class RespawnCharacterBlock : Block
	{
		public RespawnCharacterBlock()
		{
			Name = "Respawn Characters";
		}

		protected override bool OnRun()
		{
			var tickGetter = Context.GetExternal<StandardTickGetter>();
			var worldCtx   = Context.GetExternal<WorldContext>();
			var entMgr     = worldCtx.EntityMgr;
			var query      = Context.GetExternal<QueryContext>();
			if (query == null)
				throw new InvalidOperationException("Context not added!");

			using (var entities = query.Character.ToEntityArray(Allocator.TempJob))
			using (var healthArray = query.Character.ToComponentDataArray<LivableHealth>(Allocator.TempJob))
			using (var dataArray = query.Character.ToComponentDataArray<DeathMatchCharacter>(Allocator.TempJob))
			{
				for (var ent = 0; ent < entities.Length; ent++)
				{
					ref var health = ref UnsafeUtilityEx.ArrayElementAsRef<LivableHealth>(healthArray.GetUnsafePtr(), ent);
					ref var data   = ref UnsafeUtilityEx.ArrayElementAsRef<DeathMatchCharacter>(dataArray.GetUnsafePtr(), ent);
					if (!health.IsDead && health.ShouldBeDead() && health.Max > 0)
					{
						Debug.Log("Eliminated!");
						health.IsDead  = true;
						data.RespawnAt = UTick.AddMsNextFrame(tickGetter.GetTick(), 2500);

						continue;
					}

					if (health.IsDead && data.RespawnAt < tickGetter.GetTick())
					{
						health.IsDead = false;
						Debug.Log("Respawn!");

						var pos                = float3.zero;
						var setFullHealthEvent = entMgr.CreateEntity(typeof(ModifyHealthEvent));
						entMgr.SetComponentData(setFullHealthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, entities[ent]));

						if (entMgr.HasComponent<ForceSpawnPosition>(entities[ent]))
						{
							pos = entMgr.GetComponentData<ForceSpawnPosition>(entities[ent]).Value;
						}

						entMgr.SetComponentData(entities[ent], new Translation {Value = pos});
						entMgr.SetComponentData(entities[ent], default(Velocity));
					}
				}

				query.Character.CopyFromComponentDataArray(healthArray);
				query.Character.CopyFromComponentDataArray(dataArray);
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
				typeof(DeathMatchCharacter),
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

	public class DeathMatchBotCharacterProvider : ProKitCharacterProvider
	{
		public NativeArray<float3> Points;

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			base.GetComponents(out entityComponents);

			entityComponents = entityComponents.Concat(new ComponentType[]
			{
				typeof(DeathMatchCharacter),
				typeof(BotNode),
				typeof(GhostEntity),
				//typeof(TranslationSnapshot.Exclude),
				//typeof(PredictedTranslationSnapshot.Use)
			}).ToArray();
		}

		public override void SetEntityData(Entity entity, ProKitCreateCharacter data)
		{
			base.SetEntityData(entity, data);

			var buffer = EntityManager.GetBuffer<BotNode>(entity).Reinterpret<float3>();
			buffer.AddRange(Points);
		}
	}
	
	public class DeathMatchTeamProvider : BaseProviderBatch<DeathMatchTeamProvider.Create>
	{
		public Entity CurrentPlayer;
		
		public struct Create
		{

		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(TeamDescription),
				typeof(TeamAllies),
				typeof(TeamEnemies),
				typeof(GhostEntity),
				typeof(PlayEntityTag),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, CurrentPlayer);
			EntityManager.SetComponentData(entity, new Relative<PlayerDescription>(CurrentPlayer));
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.ProcessEvents))]
	public class DeathMatchEventSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			World.GetExistingSystem<GameEventRuleSystemGroup>().Process();
			
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
		public EntityQuery Character;
	}

	public class StandardTickGetter : ExternalContextBase, ITickGetter
	{
		public ServerSimulationSystemGroup Group;
		
		public UTick GetTick() => Group.GetServerTick();
	}
}