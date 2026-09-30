using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.Pooling
{
    /// <summary>
    /// Pools instances of one view prefab under a root GameObject created on first use.
    /// Safe to dispose during scene teardown, when Unity may already have destroyed the root and its views.
    /// </summary>
    public sealed class ViewPool<TView> : IDisposable where TView : Component
    {
        private readonly TView _prefab;
        private readonly string _rootName;
        private readonly ObjectPool<TView> _pool;
        private Transform _root;

        public ViewPool(TView prefab, string rootName, Action<TView> hide, int maxSize)
        {
            _prefab = prefab;
            _rootName = rootName;
            _pool = new ObjectPool<TView>(Create,
                actionOnRelease: hide,
                actionOnDestroy: DestroyView,
                collectionCheck: false,
                maxSize: maxSize);
        }

        public TView Get()
        {
            return _pool.Get();
        }

        public void Release(TView view)
        {
            // Scene teardown can destroy the view before the owning service is disposed; Hide would throw.
            if (view == null)
            {
                return;
            }

            _pool.Release(view);
        }

        public void Dispose()
        {
            _pool.Dispose();
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        private TView Create()
        {
            if (_root == null)
            {
                _root = new GameObject(_rootName).transform;
            }

            return Object.Instantiate(_prefab, _root);
        }

        private static void DestroyView(TView view)
        {
            // Idle views may already be destroyed with the scene; view.gameObject would throw.
            if (view != null)
            {
                Object.Destroy(view.gameObject);
            }
        }
    }
}
