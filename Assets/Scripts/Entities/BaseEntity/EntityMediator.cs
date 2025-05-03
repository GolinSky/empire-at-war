using System;
using System.Collections.Generic;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IEntityMediator : IService
    {
        void AddEntity(IEntity entity);
        void RemoveEntity(IEntity entity);
        IEntity GetEntity(long entityId);
    }

    public class EntityMediator : Service, IEntityMediator
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
    }
}