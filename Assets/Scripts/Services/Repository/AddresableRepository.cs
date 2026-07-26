using UnityEngine;
using UnityEngine.AddressableAssets;
using EmpireAtWar.Mvc;
using EmpireAtWar.Repository.Data;

namespace EmpireAtWar.Repository
{
    public class AddressableRepository : IRepository
    {
        private const string MAPPING_DATA_KEY = nameof(AssetMappingData);
        private AssetMappingData _mappingData;
        private bool _mappingLoadAttempted;

        public TSource Load<TSource>(string key) where TSource : Object
        {
            return Addressables.LoadAssetAsync<TSource>(ResolveKey(key)).WaitForCompletion();
        }

        public TComponent LoadComponent<TComponent>(string key) where TComponent : Component
        {
            GameObject prefab = LoadPrefab(key);
            return prefab != null ? prefab.GetComponent<TComponent>() : null;
        }
        
        
        public GameObject LoadPrefab(string key)
        {
            return Addressables.LoadAssetAsync<GameObject>(ResolveKey(key)).WaitForCompletion();
        }

        private string ResolveKey(string key)
        {
            if (key == MAPPING_DATA_KEY)
                return key;

            AssetMappingData mappingData = GetMappingData();
            return mappingData != null ? mappingData.GetAssetKey(key) : key;
        }

        private AssetMappingData GetMappingData()
        {
            if (_mappingLoadAttempted)
                return _mappingData;

            _mappingLoadAttempted = true;
            _mappingData = Addressables
                .LoadAssetAsync<AssetMappingData>(MAPPING_DATA_KEY)
                .WaitForCompletion();

            if (_mappingData == null)
                Debug.LogWarning($"[{nameof(AddressableRepository)}] {MAPPING_DATA_KEY} was not found. Falling back to the requested Addressables keys.");

            return _mappingData;
        }
    }
}
