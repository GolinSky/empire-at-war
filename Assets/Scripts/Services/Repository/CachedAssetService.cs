using System;
using System.Collections.Generic;
using System.Threading;
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
            (Type, string) cacheKey = (typeof(TSource), key);
            AsyncOperationHandle handle = GetOrLoad<TSource>(cacheKey);
            try
            {
                if (!handle.IsDone) handle.WaitForCompletion();
                return GetResult<TSource>(key, handle);
            }
            catch
            {
                Release(cacheKey, handle);
                throw;
            }
        }

        public async Awaitable PreloadAsync<TSource>(string key, CancellationToken cancellationToken)
            where TSource : Object
        {
            cancellationToken.ThrowIfCancellationRequested();
            (Type, string) cacheKey = (typeof(TSource), key);
            AsyncOperationHandle handle = GetOrLoad<TSource>(cacheKey);
            try
            {
                while (!handle.IsDone)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                    ThrowIfDisposed();
                }
                cancellationToken.ThrowIfCancellationRequested();
                GetResult<TSource>(key, handle);
            }
            catch
            {
                Release(cacheKey, handle);
                throw;
            }
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

        private AsyncOperationHandle GetOrLoad<TSource>((Type, string) cacheKey) where TSource : Object
        {
            ThrowIfDisposed();
            if (!_handles.TryGetValue(cacheKey, out AsyncOperationHandle handle))
            {
                handle = _loader.Load<TSource>(cacheKey.Item2);
                _handles.Add(cacheKey, handle);
            }
            return handle;
        }

        private static TSource GetResult<TSource>(string key, AsyncOperationHandle handle) where TSource : Object
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                throw new InvalidOperationException($"Failed to load '{key}' as {typeof(TSource).Name}.", handle.OperationException);
            return (TSource)handle.Result;
        }

        private void Release((Type, string) cacheKey, AsyncOperationHandle handle)
        {
            if (_handles.TryGetValue(cacheKey, out AsyncOperationHandle ownedHandle) && ownedHandle.Equals(handle))
            {
                _handles.Remove(cacheKey);
                handle.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CachedAssetService));
        }
    }
}
