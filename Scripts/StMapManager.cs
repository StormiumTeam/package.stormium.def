using System;
using System.Collections.Generic;
using Unity.Entities;

namespace package.stormium.def
{
    public struct StormiumMapAssetInformation
    {
        public bool IsCreated;
        
        public string Id;
        public string Name;
        public Type   GamemodeType;

        public StormiumMapAssetInformation(string id, string name, Type gamemodeType)
        {
            IsCreated = true;
            
            Id           = id;
            Name         = name;
            GamemodeType = gamemodeType;
        }
    }
    
    public struct MapManagerData : IComponentData
    {
        public int CurrentMapReferenceId;
    }

    public struct MapManagerCatalogElement : IBufferElementData
    {
        public int MapReferenceId;
    }
    
    public class StMapManager : ComponentSystem
    {
        public struct KvpMapAssetInformation
        {
            public bool IsCreated;
            
            public int LocalId;
            public StormiumMapAssetInformation Map;
        }

        public event Action<StormiumMapAssetInformation> OnRequestMapLoad;
        
        public Dictionary<string, KvpMapAssetInformation> AllMapAssetInformations;
        
        public Entity LocalMapManagerData;

        protected override void OnCreateManager()
        {
            AllMapAssetInformations = new Dictionary<string, KvpMapAssetInformation>(16);
            LocalMapManagerData = EntityManager.CreateEntity(typeof(MapManagerData), typeof(MapManagerCatalogElement));
        }

        protected override void OnUpdate()
        {
            
        }

        public void LoadMapById(string id)
        {
            if (!AllMapAssetInformations.ContainsKey(id))
                return;

            var data = EntityManager.GetComponentData<MapManagerData>(LocalMapManagerData);

            data.CurrentMapReferenceId = AllMapAssetInformations[id].LocalId;
            
            EntityManager.SetComponentData(LocalMapManagerData, data);
            
            OnRequestMapLoad?.Invoke(AllMapAssetInformations[id].Map);
        }

        public void AddMapToLocalCatalog(StormiumMapAssetInformation assetInformation, bool force = false)
        {
            var existingKvp = default(KvpMapAssetInformation);
            
            if (!force && AllMapAssetInformations.TryGetValue(assetInformation.Id, out existingKvp))
                return;

            var newLocalId = AllMapAssetInformations.Count + 1;
            if (existingKvp.IsCreated)
            {
                newLocalId = existingKvp.LocalId;
            }

            AllMapAssetInformations[assetInformation.Id] = new KvpMapAssetInformation
            {
                IsCreated = true,
                LocalId   = newLocalId,
                Map       = assetInformation
            };
            
            var catalogBuffer = EntityManager.GetBuffer<MapManagerCatalogElement>(LocalMapManagerData);
            
            catalogBuffer.Clear();

            foreach (var mapInformation in AllMapAssetInformations)
            {
                catalogBuffer.Add(new MapManagerCatalogElement {MapReferenceId = mapInformation.Value.LocalId});
            }
        }
    }
}