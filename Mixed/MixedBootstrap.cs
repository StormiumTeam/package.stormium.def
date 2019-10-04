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
                foreach (var type in GetTypes(typeof(ISystemDelegateForSnapshot), typeof(ComponentSystemBase))
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"{i}-snapshot:{type}");
                    builder.Add(world.GetOrCreateSystem(type));
                    i++;
                }

                foreach (var type in GetTypes(typeof(IEntityDescription), null)
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"{i}-snapshot:desc:{type}");
                    builder.Add(world.GetOrCreateSystem(typeof(ComponentSnapshotSystemTag<>).MakeGenericType(type)));
                    i++;
                }
            });
            World.GetOrCreateSystem<RpcCollectionSystem>().SetFixedCollection((world, builder) =>
            {
                foreach (var type in GetTypes(typeof(IRpcCommand), null)
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"rpc:{type}");
                    try
                    {
                        builder.Add((RpcProcessSystemBase) world.GetOrCreateSystem(typeof(DefaultRpcProcessSystem<>).MakeGenericType(type)));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error at making {type}");
                        throw;
                    }
                }
            });
            World.GetOrCreateSystem<CommandCollectionSystem>().SetFixedCollection((world, builder) =>
            {
                foreach (var type in GetTypes(typeof(ICommandData<>), null)
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"cmd:{type}");
                    try
                    {
                        builder.Add((CommandProcessSystemBase) world.GetOrCreateSystem(typeof(DefaultCommandProcessSystem<>).MakeGenericType(type)));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error at making {type}");
                        throw;
                    }
                }
            });
        }

        protected override void OnUpdate()
        {

        }

        private static IEnumerable<Type> GetTypes(Type interfaceType, Type subclass)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var validAssemblies = new List<Assembly>();
            foreach (var asm in assemblies)
            {
                try
                {
                    asm.GetTypes();
                    validAssemblies.Add(asm);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error from assembly: {asm.FullName}");
                }
            }
            
            return from asm in validAssemblies
                   from type in asm.GetTypes()
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