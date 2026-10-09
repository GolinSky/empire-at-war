using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceAssetResolver
    {
        public static T Load<T>(string canonicalKey) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(canonicalKey);
            if (path.Length == 0)
            {
                var entries = AddressableAssetSettingsDefaultObject.Settings.groups.Where(group => group != null)
                    .SelectMany(group => group.entries).Where(entry => entry.address == canonicalKey).ToList();
                if (entries.Count != 1) throw new InvalidOperationException($"Canonical Addressables key {canonicalKey} resolves to {entries.Count} entries.");
                path = entries[0].AssetPath;
            }
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new UnityEngine.MissingReferenceException($"Missing {typeof(T).Name} at canonical key {canonicalKey} ({path}).");
            return asset;
        }
    }
}
