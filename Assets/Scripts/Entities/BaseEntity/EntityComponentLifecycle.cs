using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.BaseEntity
{
    public sealed class EntityComponentLifecycle
    {
        private readonly IReadOnlyList<IMonoComponent> _components;
        private bool _isReleased;

        public EntityComponentLifecycle(IReadOnlyList<IMonoComponent> components)
        {
            _components = components;
        }

        public bool Release()
        {
            if (_isReleased)
            {
                return false;
            }

            _isReleased = true;
            foreach (IMonoComponent component in _components)
            {
                component.Release();
            }

            return true;
        }
    }
}
