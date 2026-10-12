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

        /// <summary>Lets the components be released again in a reused entity's next life.</summary>
        public void Restart() => _isReleased = false;

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
