using CharacterController;
using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.Client.Visual
{
	public class DefaultCharacterPresentation : CharacterPresentationBase
	{
		public Animator Animator;
		
		public GameObject FirstPerson;
		public GameObject ThirdPerson;

		public float2 AMove;
		public float  ARun;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(SpawnCharacterBackendSystem))]
	public class SetCharacterPresentation : GameBaseSystem
	{
		private                 AsyncAssetPool<GameObject> m_Pool;
		private static readonly int                        s_Movx = Animator.StringToHash("movx");
		private static readonly int                        s_Movy = Animator.StringToHash("movy");
		private static readonly int                        s_Run  = Animator.StringToHash("run");

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Pool = new AsyncAssetPool<GameObject>("def/visuals/Character/Prototype/Character.prefab");
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((CharacterBackend backend) =>
			{
				if (backend.Presentation is DefaultCharacterPresentation presentation)
				{
					Execute(backend, presentation);
					return;
				}

				backend.SetPresentationFromPool(m_Pool);
			});
		}

		private void Execute(CharacterBackend backend, DefaultCharacterPresentation presentation)
		{
			var ent = backend.DstEntity;
			if (!EntityManager.HasComponent<CharacterInput>(ent))
				return;

			var input = EntityManager.GetComponentData<CharacterInput>(ent);
			var vel   = EntityManager.GetComponentData<Velocity>(ent);

			presentation.AMove = Vector2.MoveTowards(presentation.AMove, input.Move, Time.DeltaTime * 5f);
			presentation.AMove = Vector2.Lerp(presentation.AMove, input.Move, Time.DeltaTime * 12.5f);
			presentation.ARun  = math.clamp(math.length(vel.xfz) - 7.5f, 0, 1);

			var rot =  ((Quaternion) EntityManager.GetComponentData<Rotation>(ent).Value).normalized;
			var eulerAngles = rot.eulerAngles;
			eulerAngles.x = 0;
			eulerAngles.z = 0;
			rot.eulerAngles = eulerAngles;

			presentation.transform.localPosition = EntityManager.GetComponentData<Translation>(ent).Value - new float3(0, 0.5f, 0);
			
			presentation.transform.localRotation = rot;

			presentation.Animator.SetFloat(s_Movx, presentation.AMove.x);
			presentation.Animator.SetFloat(s_Movy, presentation.AMove.y);
			presentation.Animator.SetFloat(s_Run, presentation.ARun);

			var player = GetFirstSelfGamePlayer();
			if (!TryGetCurrentCameraState(player, out var cameraState))
				return;

			var isDead = EntityManager.GetComponentData<LivableHealth>(ent).IsDead;
			presentation.ThirdPerson.SetActive(cameraState.Target != ent && !isDead);
			presentation.FirstPerson.SetActive(cameraState.Target == ent && !isDead);
		}
	}
}