using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using EmpireAtWar.Repository.Data;

namespace EmpireAtWar.Repository
{
    public class AddressableHandleLoader : IAssetHandleLoader
    {
        private const string MAPPING_DATA_KEY = nameof(AssetMappingData);

        private AssetMappingData _mappingData;

        private bool _mappingLoadAttempted;

        public AsyncOperationHandle<TSource> Load<TSource>(string key) where TSource : Object
        {
            return Addressables.LoadAssetAsync<TSource>(ResolveKey(key));
        }

        private string ResolveKey(string key)
        {
            if (key == MAPPING_DATA_KEY)
                return key;

            AssetMappingData mappingData = GetMappingData();
            return mappingData.GetAssetKey(key);
        }

        // The mapping handle is kept for the loader's lifetime, which is the application's.
        private AssetMappingData GetMappingData()
        {
            if (!_mappingLoadAttempted)
            {
                _mappingLoadAttempted = true;
                _mappingData = Addressables
                    .LoadAssetAsync<AssetMappingData>(MAPPING_DATA_KEY)
                    .WaitForCompletion();
            }

            if (_mappingData == null)
                throw new System.InvalidOperationException($"[{nameof(AddressableHandleLoader)}] Required {MAPPING_DATA_KEY} could not be loaded.");

            return _mappingData;
        }
    }
}
