using UnityEngine;
using UnityEngine.AddressableAssets;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Repository.Data
{
    [CreateAssetMenu(fileName = nameof(AssetMappingData), menuName = "Data/Asset Mapping Data")]
    public class AssetMappingData : ScriptableObject
    {
        [Tooltip("Maps the key used by code (usually a C# type name) to an Addressable asset reference.")]
        [SerializeField]
        private DictionaryWrapper<string, AssetReference> assetMappings = new();

        public string GetAssetKey(string key)
        {
            if (assetMappings != null &&
                assetMappings.Dictionary.TryGetValue(key, out AssetReference mappedReference) &&
                mappedReference != null &&
                !string.IsNullOrEmpty(mappedReference.AssetGUID))
            {
                return mappedReference.AssetGUID;
            }

            return key;
        }
    }
}
