using System.Runtime.InteropServices;
using LiteNetLib;
using package.stormium.core;
using package.stormium.def.actions;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace package.stormium.def.Projectiles
{
    [AlwaysUpdateSystem]
    public class StDefProjectileRocketProcessSystem : StProjectileSystem, EventReceiveData.IEv
    {
        public readonly MessageIdent MsgTestCreateProj;
        public readonly MessageIdent MsgStartProjectile, MsgEndProjectile;

        public string AddressClientProjectileRocketExplosion;

        public GameObject[] ClientProjectileRocketExplosion;

        protected struct Group
        {
            public ComponentDataArray<ProjectileTag>         ProjectileTag;
            public ComponentDataArray<ProjectileOwner>       OwnerArray;
            public ComponentDataArray<Position>              PositionArray;
            public ComponentDataArray<StVelocity>            VelocityArray;
            public ComponentDataArray<StDefProjectileRocket> RocketArray;
            public BufferArray<IgnoreCollisionElement>       IgnoreCollisionBufferArray;
            public EntityArray                               Entities;

            public readonly int Length;
        }

        [Inject] private Group           m_ProjectilesGroup;
        private          RaycastHit[]    m_BufferHits;
        private          EntityArchetype m_EntityArchetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_BufferHits                    = new RaycastHit[64];
            ClientProjectileRocketExplosion = new GameObject[8];

            m_EntityArchetype = EntityManager.CreateArchetype
            (
                typeof(ProjectileTag),
                typeof(ProjectileOwner),
                typeof(Position),
                typeof(NetPosition),
                typeof(StVelocity),
                typeof(StDefProjectileRocket),
                typeof(IgnoreCollisionElement)
            );
        }

        private Mesh     m_ProtoMesh;
        private Material m_Material;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            Addressables.LoadAsset<GameObject>("int:projectiles/Rocket/RocketExplosion").Completed += op =>
            {
                for (int i = 0; i != ClientProjectileRocketExplosion.Length; i++)
                {
                    ClientProjectileRocketExplosion[i] = Object.Instantiate(op.Result);
                }
            };

            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            m_ProtoMesh = Object.Instantiate(gameObject.GetComponent<MeshFilter>().sharedMesh);

            Object.Destroy(gameObject);

            m_Material = new Material(Shader.Find("HDRenderPipeline/Lit"));
            m_Material.SetColor("_BaseColor", Color.black);
            m_Material.enableInstancing = true;
        }

        protected override void OnUpdate()
        {
            var physicUpdater = World.Active.GetOrCreateManager<PhysicUpdaterSystem>();

            if (!GameLaunch.IsServer)
            {
                ProcessClient(physicUpdater.LastFixedTimeStep, physicUpdater.LastIterationCount);
            }

            if (CanExecuteServerActions)
            {
                ProcessServer(physicUpdater.LastFixedTimeStep, physicUpdater.LastIterationCount);
            }
        }

        protected void ProcessClient(float stepDelta, int stepCount)
        {
            for (int i = 0; i != m_ProjectilesGroup.Length; i++)
            {
                var matrix1 = Matrix4x4.TRS(m_ProjectilesGroup.PositionArray[i].Value, Quaternion.identity, Vector3.one * 0.25f);
                Graphics.DrawMeshInstanced(m_ProtoMesh, 0, m_Material, new[] {matrix1}, 1);
            }

            if (Input.GetMouseButtonDown(1))
            {
                var camTr = Camera.main.transform;

                var data = CreateMessage(MsgTestCreateProj);

                data.Put(camTr.position);
                data.Put(camTr.forward);

                SendToServer(data);

                Debug.Log("Sending a new weapon to create!");
            }
        }

        protected void ProcessServer(float stepDelta, int stepCount)
        {
            stepCount = 1;
            
            for (int i = 0; i != stepCount; i++)
            {
                ProcessServerPhysics(stepDelta);
            }
        }

        protected void ProcessServerPhysics(float delta)
        {
            for (int i = 0; i != m_ProjectilesGroup.Length; i++)
            {
                var positionData = m_ProjectilesGroup.PositionArray[i];
                var velocityData = m_ProjectilesGroup.VelocityArray[i];
                var entity       = m_ProjectilesGroup.Entities[i];

                var explode = false;

                var targetPosition = positionData.Value + (float3) velocityData.Value * delta;
                var targetDir      = (Vector3) (targetPosition - positionData.Value);

                var ray    = new Ray(positionData.Value, velocityData.Value.normalized);
                var length = Physics.RaycastNonAlloc(ray, m_BufferHits, velocityData.Value.magnitude * delta, CPhysicSettings.PhysicInteractionLayerMask);
                for (int bIndex = 0; bIndex != length; bIndex++)
                {
                    var rayHit = m_BufferHits[bIndex];
                    if (!(rayHit.collider is CharacterController))
                    {
                        explode = true;
                    }
                }

                if (explode)
                {
                    PostUpdateCommands.DestroyEntity(entity);

                    var data = CreateMessage(MsgEndProjectile);
                    data.Put(entity);
                    
                    ServerSendToAll(data);
                }
                else
                {
                    PostUpdateCommands.SetComponent(entity, new Position {Value = targetPosition});
                }
            }
        }

        public Entity CreateProjectile(EntityCommandBuffer ecb)
        {
            Debug.Assert(CanExecuteServerActions, "CanExecuteServerActions");

            if (!CanExecuteServerActions) return Entity.Null;

            var entity = EntityManager.CreateEntity(m_EntityArchetype);
            
            ServerEntityMgr.ConvertAsNetworkable(ecb, entity, entity);

            Debug.Log("Create a projectile!");

            return entity;
        }

        public void SendProjectile(Entity entity)
        {
            var ownerComponent    = EntityManager.GetComponentData<ProjectileOwner>(entity);
            var velocityComponent = EntityManager.GetComponentData<StVelocity>(entity);
            var positionComponent = EntityManager.GetComponentData<Position>(entity);

            ServerEntityMgr.BroadcastEntity(entity.GetComponentData<NetworkEntity>(), EntityType.PureEntity, true);

            var data = CreateMessage(MsgStartProjectile);
            data.Put(entity);
            data.Put(ownerComponent.Target);
            data.Put(positionComponent.Value);
            data.Put(velocityComponent.Value);

            ServerSendToAll(data);

            Debug.Log("Sending a projectile! " + entity);
        }

        public void Callback(EventReceiveData.Arguments args)
        {
            if (!args.Reader.Type.IsPattern()) return;

            var conMsgMgr = args.PeerInstance.GetPatternManager();

            var msg = conMsgMgr.GetPattern(args.Reader);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            if (msg == MsgTestCreateProj)
            {
                var position = args.Reader.Data.GetVec3();
                var aim      = args.Reader.Data.GetVec3();
                var proj = CreateProjectile(ecb);

                EntityManager.SetComponentData(proj, new Position {Value = position});
                EntityManager.SetComponentData(proj, new StVelocity(aim * 42f));
                
                Debug.Log($"Creating a new projectile. P: {position}; A: {aim}");

                SendProjectile(proj);
            }
            else if (msg == MsgStartProjectile)
            {
                var serverEntity = args.Reader.GetEntity();
                var owner        = default(Entity);
                var position     = args.Reader.Data.GetVec3();
                var velocity     = args.Reader.Data.GetVec3();

                var clientEntity = EntityManager.CreateEntity(m_EntityArchetype);

                    ServerEntityMgr.ConvertAsNetworkable(ecb, clientEntity, serverEntity);

                EntityManager.SetOrAddComponentData(clientEntity, new ProjectileTag());
                EntityManager.SetOrAddComponentData(clientEntity, new ProjectileOwner(owner));
                EntityManager.SetOrAddComponentData(clientEntity, new Position {Value = position});
                EntityManager.SetOrAddComponentData(clientEntity, new StVelocity(velocity));

                Debug.Log("We got a new projectile from server!");
            }
            else if (msg == MsgEndProjectile)
            {
                var serverEntity = args.Reader.GetEntity();
                var clientEntity = GetEntity(serverEntity);

                Debug.Log($"Destroying projectile... c: {clientEntity}, s: {serverEntity}");
                
                ServerEntityMgr.DestroyEntity(serverEntity);
                ecb.DestroyEntity(clientEntity);
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}