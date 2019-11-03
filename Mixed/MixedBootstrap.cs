using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Karambolo.Common;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
    public class SetCollectionSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            World.GetOrCreateSystem<SnapshotManager>().SetFixedSystemsFromBuilder((world, builder) =>
            {
                var i = 1;
                foreach (var type in GetTypes(typeof(ISystemDelegateForSnapshot), typeof(ComponentSystemBase)))
                {
                    Debug.Log($"System#{i} --> {type}");
                    builder.Add(world.GetOrCreateSystem(type));
                    i++;
                }

                foreach (var type in GetTypes(typeof(IEntityDescription), null))
                {
                    builder.Add(world.GetOrCreateSystem(typeof(ComponentSnapshotSystemTag<>).MakeGenericType(type)));
                    i++;
                }
            });
            World.GetOrCreateSystem<RpcCollectionSystem>().SetFixedCollection((world, builder) =>
            {
                foreach (var type in GetTypes(typeof(IRpcCommandRequestComponentData), null))
                {
                    try
                    {
                        builder.Add((RpcProcessSystemBase) world.GetOrCreateSystem(typeof(RpcCommandRequest<>).MakeGenericType(type)));
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            });
            World.GetOrCreateSystem<CommandCollectionSystem>().SetFixedCollection((world, builder) =>
            {
                foreach (var type in GetTypes(typeof(ICommandData), null))
                {
                    try
                    {
                        builder.Add((CommandProcessSystemBase) world.GetOrCreateSystem(typeof(DefaultCommandProcessSystem<>).MakeGenericType(type)));
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            });
        }

        protected override void OnUpdate()
        {

        }

        private static List<Type> m_AssembliesTypes;

        private static IEnumerable<Type> GetTypes(Type interfaceType, Type subclass)
        {
            if (m_AssembliesTypes == null)
            {
                m_AssembliesTypes = new List<Type>(1024);

                var assemblies      = AppDomain.CurrentDomain.GetAssemblies();
                var validAssemblies = new List<Assembly>();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        validAssemblies.Add(asm);
                        m_AssembliesTypes.AddRange(types);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error from assembly: {asm.FullName}");
                    }
                }

                m_AssembliesTypes = m_AssembliesTypes.OrderBy(t => t.FullName).ToList();
            }

            return from type in m_AssembliesTypes
                   where type.HasInterface(interfaceType)
                         && (subclass == null || type.IsSubclassOf(subclass))
                         && !type.IsAbstract
                   select type;
        }
    }

    public class SetVersionSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            GameStatic.Version = 2;
        }

        protected override void OnUpdate()
        {

        }
    }
}