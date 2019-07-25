using Stormium.Core;
using Stormium.Default;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Scripts.Actions.ProKitWeapons
{
	public struct ProRocketDetonateAction : IStateData, IComponentData
	{
		public class Streamer : SnapshotEntityDataAutomaticStreamer<ProRocketDetonateAction>
		{
		}

		public float cooldown;
	}

	[DisableAutoCreation]
	public class ProRocketDetonateActionUpdateSystem : GameBaseSystem
	{
		[BurstCompile]
		struct JobUpdate : IJobChunk
		{
			public int Tick;

			[ReadOnly] public ArchetypeChunkComponentType<StActionSlotInput>              InputType;
			[ReadOnly] public ArchetypeChunkComponentType<Relative<LivableDescription>> LivableOwnerType;

			// narrow foreach²
			[DeallocateOnJobCompletion]
			public NativeArray<ArchetypeChunk> NarrowProjectileChunks;

			public ArchetypeChunkComponentType<ProProjectile.PredictedState> ProjectilePredictedType;

			public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var inputArray        = chunk.GetNativeArray(InputType);
				var livableOwnerArray = chunk.GetNativeArray(LivableOwnerType);
				var count             = chunk.Count;

				for (var i = 0; i != count; i++)
				{
					if (!inputArray[i].IsActive)
						continue;

					var owner = livableOwnerArray[i];

					var enumerator = NarrowProjectileChunks.GetEnumerator();
					while (enumerator.MoveNext())
					{
						var narrowChunk = enumerator.Current;

						var projectilePredictedArray = chunk.GetNativeArray(ProjectilePredictedType);
						var rocketLivableOwnerArray  = chunk.GetNativeArray(LivableOwnerType);
						var narrowCount              = narrowChunk.Count;

						for (var n = 0; n != narrowCount; n++)
						{
							var     rocketOwner    = rocketLivableOwnerArray[n];
							ref var predictedState = ref UnsafeUtilityEx.ArrayElementAsRef<ProProjectile.PredictedState>(projectilePredictedArray.GetUnsafePtr(), n);

							if (rocketOwner.Target == owner.Target)
							{
								predictedState.endTick = Tick;
								predictedState.phase   = StandardProjectilePhase.Ended;
								predictedState.endType = StandardProjectileEndType.Collision;
							}
						}
					}

					enumerator.Dispose();
				}
			}
		}

		private EntityQuery m_DetonateGroup, m_RocketGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			var query = new EntityQueryDesc
			{
				All = new[] {ComponentType.ReadOnly<ProRocketDetonateAction>(), ComponentType.ReadOnly<StActionSlotInput>(), ComponentType.ReadOnly<Relative<LivableDescription>>()}
			};
			m_DetonateGroup = GetEntityQuery(query);

			query = new EntityQueryDesc
			{
				All = new[] {ComponentType.ReadOnly<ProRocketProjectile>(), ComponentType.ReadOnly<Relative<LivableDescription>>(), ComponentType.ReadWrite<ProProjectile.PredictedState>()}
			};
			m_RocketGroup = GetEntityQuery(query);
		}

		protected override void OnUpdate()
		{
			var rocketChunks = m_RocketGroup.CreateArchetypeChunkArray(Allocator.TempJob);

			var job = new JobUpdate
			{
				Tick                    = Tick,
				InputType               = GetArchetypeChunkComponentType<StActionSlotInput>(true),
				LivableOwnerType        = GetArchetypeChunkComponentType<Relative<LivableDescription>>(true),
				NarrowProjectileChunks  = rocketChunks,
				ProjectilePredictedType = GetArchetypeChunkComponentType<ProProjectile.PredictedState>(false)
			};

			if (SystemGroup_CanHaveDependency())
				SetDependency(JobChunkExtensions.Schedule(job, m_DetonateGroup, GetDependency()));
			else
				job.Run(m_DetonateGroup);
		}
	}
}