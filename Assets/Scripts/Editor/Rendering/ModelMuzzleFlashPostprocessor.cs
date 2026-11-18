using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>Model-embedded flashes are replaced by the weapon shot effects.</summary>
    public sealed class ModelMuzzleFlashPostprocessor : AssetPostprocessor
    {
        public override uint GetVersion() => 2;

        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith("Assets/Art/Models/", StringComparison.Ordinal)) return;
            var flashMeshes = new HashSet<Mesh>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0)
                    flashMeshes.Add(filter.sharedMesh);
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.sharedMesh.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0)
                    flashMeshes.Add(renderer.sharedMesh);
            Strip(root.transform);
            // Unity retains imported mesh subassets even after their GameObjects are removed.
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                flashMeshes.Remove(filter.sharedMesh);
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                flashMeshes.Remove(renderer.sharedMesh);
            foreach (Mesh mesh in flashMeshes)
                UnityEngine.Object.DestroyImmediate(mesh);
        }

        public static int Strip(Transform root)
        {
            int removed = 0;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                removed += Strip(child);
                if (child.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Keep any authored fire points beneath a flash, with the same world pose.
                while (child.childCount > 0)
                    child.GetChild(0).SetParent(root, true);
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                removed++;
            }
            return removed;
        }
    }
}
