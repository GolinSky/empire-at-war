using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using EmpireAtWar.Mvc;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Repository
{
    // Holds one Addressables handle per (type, key) for its owning scope and releases them all on Dispose.
    public sealed class CachedAssetService : IAssetService, IDisposable
    {
        private readonly IAssetHandleLoader _loader;
        private readonly Dictionary<(Type, string), AsyncOperationHandle> _handles =
            new Dictionary<(Type, string), AsyncOperationHandle>();
        private bool _isDisposed;

        public CachedAssetService(IAssetHandleLoader loader)
        {
            _loader = loader;
        }

        public TSource Load<TSource>(string key) where TSource : Object
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CachedAssetService), $"Cannot load '{key}' after its scope released its assets.");

            (Type, string) cacheKey = (typeof(TSource), key);
            if (!_handles.TryGetValue(cacheKey, out AsyncOperationHandle handle))
            {
                handle = _loader.Load<TSource>(key);
                handle.WaitForCompletion();
                _handles.Add(cacheKey, handle);
            }
            return (TSource)handle.Result;
        }

        public TComponent LoadComponent<TComponent>(string key) where TComponent : Component
        {
            GameObject prefab = LoadPrefab(key);
            if (prefab == null || !prefab.TryGetComponent(out TComponent component))
                throw new MissingComponentException($"Addressable prefab '{key}' has no {typeof(TComponent).Name}.");
            return component;
        }

        public GameObject LoadPrefab(string key) => Load<GameObject>(key);

        public void Dispose()
        {
            _isDisposed = true;
            foreach (AsyncOperationHandle handle in _handles.Values) handle.Release();
            _handles.Clear();
        }
    }
}
