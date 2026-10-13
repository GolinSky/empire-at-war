using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    [LifecycleOrder(Dispose = 1000)]
    public sealed class EntityScopeOwnership : IOrderedDisposable
    {
        private readonly List<EntityLifetimeScope> _scopes = new List<EntityLifetimeScope>();

        public int Count => _scopes.Count;

        public void Register(EntityLifetimeScope scope) => _scopes.Add(scope);

        public void Unregister(EntityLifetimeScope scope) => _scopes.Remove(scope);

        public void Dispose()
        {
            EntityLifetimeScope[] scopes = _scopes.ToArray();
            _scopes.Clear();
            for (int index = scopes.Length - 1; index >= 0; index--)
            {
                EntityLifetimeScope scope = scopes[index];
                try
                {
                    scope.DisposeCore();
                }
                finally
                {
                    if (Application.isPlaying) Object.Destroy(scope.gameObject);
                    else Object.DestroyImmediate(scope.gameObject);
                }
            }
        }
    }
}
