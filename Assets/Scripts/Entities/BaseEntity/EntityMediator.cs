using System;
using System.Collections.Generic;
using LightWeightFramework.Components.Service;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IEntityLocator : IService
    {
        void AddEntity(IEntity entity);
        void RemoveEntity(IEntity entity);
        IEntity GetEntity(long entityId);
        
        bool TryGetEntity(RaycastHit raycastHit, out IEntity entity);
    }

    public class EntityLocator : Service, IEntityLocator
    {
        private Dictionary<long, IEntity> _entities = new Dictionary<long, IEntity>();
        
        public void AddEntity(IEntity entity)
        {
            _entities.Add(entity.Id, entity);
        }

        public void RemoveEntity(IEntity entity)
        {
            _entities.Remove(entity.Id);
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
            entity = null;
            IViewEntity viewEntity = raycastHit.collider.GetComponent<IViewEntity>();
            if (viewEntity != null)
            {
                entity = GetEntity(viewEntity.Id);
                return entity != null;
            }

            return false;
        }
    }
}