using System;
using System.Threading;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using EmpireAtWar.Repository.Data;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Repository
{
    public class AddressableHandleLoader : IAssetHandleLoader, IDisposable
    {
        private const string MAPPING_DATA_KEY = nameof(AssetMappingData);
        private static readonly ProfilerMarker _mappingMarker = new ProfilerMarker("AddressableHandleLoader.GetMappingData");

        private AssetMappingData _mappingData;
        private AsyncOperationHandle<AssetMappingData> _mappingHandle;

        private bool _mappingLoadAttempted;
        private bool _isDisposed;

        public async Awaitable InitializeAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (_mappingData != null) return;
            BeginMappingLoad();
            AsyncOperationHandle<AssetMappingData> handle = _mappingHandle;
            try
            {
                while (!handle.IsDone)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                    ThrowIfDisposed();
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                    throw new InvalidOperationException($"Required {MAPPING_DATA_KEY} could not be loaded.", handle.OperationException);
                _mappingData = handle.Result;
            }
            catch
            {
                ReleaseMapping();
                throw;
            }
        }

        public AsyncOperationHandle<TSource> Load<TSource>(string key) where TSource : Object
        {
            ThrowIfDisposed();
            return Addressables.LoadAssetAsync<TSource>(ResolveKey(key));
        }

        private string ResolveKey(string key)
        {
            if (key == MAPPING_DATA_KEY)
                return key;

            AssetMappingData mappingData = GetMappingData();
            return mappingData.GetAssetKey(key);
        }

        private AssetMappingData GetMappingData()
        {
            using (_mappingMarker.Auto())
            {
                if (_mappingData != null) return _mappingData;
                BeginMappingLoad();
                try
                {
                    _mappingData = _mappingHandle.WaitForCompletion();
                    if (_mappingData == null)
                        throw new InvalidOperationException($"Required {MAPPING_DATA_KEY} could not be loaded.", _mappingHandle.OperationException);
                    return _mappingData;
                }
                catch
                {
                    ReleaseMapping();
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _mappingData = null;
            ReleaseMapping();
        }

        private void BeginMappingLoad()
        {
            if (!_mappingLoadAttempted)
            {
                _mappingLoadAttempted = true;
                _mappingHandle = Addressables.LoadAssetAsync<AssetMappingData>(MAPPING_DATA_KEY);
            }
            if (!_mappingHandle.IsValid())
                throw new InvalidOperationException($"Required {MAPPING_DATA_KEY} could not be loaded.");
        }

        private void ReleaseMapping()
        {
            if (_mappingHandle.IsValid()) _mappingHandle.Release();
            _mappingHandle = default;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(AddressableHandleLoader));
        }
    }
}
