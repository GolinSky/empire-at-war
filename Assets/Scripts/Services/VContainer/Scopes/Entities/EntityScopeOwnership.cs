using System.Collections.Generic;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.VContainer
{
    [LifecycleOrder(Dispose = LifecycleOrders.ENTITY_SCOPES_DISPOSE)]
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
            var failures = new CleanupFailures();
            for (int index = scopes.Length - 1; index >= 0; index--)
                failures.DisposeScope(scopes[index]);
            failures.ThrowIfAny();
        }
    }
}
