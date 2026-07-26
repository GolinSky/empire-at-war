using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IEntityLocator : IService
    {
        event Action<IEntity> EntityAdded;
        event Action<IEntity> EntityRemoved;

        IReadOnlyCollection<IEntity> Entities { get; }

        void AddEntity(IEntity entity);
        void RemoveEntity(IEntity entity);
        IEntity GetEntity(long entityId);
        
        bool TryGetEntity(RaycastHit raycastHit, out IEntity entity);
        bool TryGetEntity(Collider collider, out IEntity entity);
    }

    public class EntityLocator : Service, IEntityLocator
    {
        private readonly Dictionary<long, IEntity> _entities = new Dictionary<long, IEntity>();

        public event Action<IEntity> EntityAdded;
        public event Action<IEntity> EntityRemoved;

        public IReadOnlyCollection<IEntity> Entities => _entities.Values;
        
        public void AddEntity(IEntity entity)
        {
            _entities.Add(entity.Id, entity);
            EntityAdded?.Invoke(entity);
        }

        public void RemoveEntity(IEntity entity)
        {
            if (_entities.Remove(entity.Id))
            {
                EntityRemoved?.Invoke(entity);
            }
        }

        public IEntity GetEntity(long entityId)
        {
            if (_entities.ContainsKey(entityId))
            {
                return _entities[entityId];
            }
            throw new Exception("Not entity found with id: " + entityId);
        }

        public bool TryGetEntity(RaycastHit raycastHit, out IEntity entity)
        {
            return TryGetEntity(raycastHit.collider, out entity);
        }

        public bool TryGetEntity(Collider collider, out IEntity entity)
        {
            entity = null;
            IViewEntity viewEntity = collider.GetComponentInParent<IViewEntity>();
            return viewEntity != null && _entities.TryGetValue(viewEntity.Id, out entity);
        }
    }
}
