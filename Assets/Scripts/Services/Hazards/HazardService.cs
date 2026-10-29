using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Hazards
{
    public sealed class HazardService : IHazardService, IInitializable, ITickable,
        ILateDisposable, EmpireAtWar.IObserver<BattleMap>
    {
        private const float UPDATE_INTERVAL = 0.1f;
        private readonly IEntityLocator _entities;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly List<IHazardSource> _sources = new List<IHazardSource>();
        private readonly Dictionary<CombatModifiers, List<IHazardSource>> _memberships =
            new Dictionary<CombatModifiers, List<IHazardSource>>();
        private readonly Dictionary<IEntity, Action> _destroyHandlers = new Dictionary<IEntity, Action>();
        private float _remainingTime;

        public event Action<IEntity, SpaceHazardData> Entered;
        public IReadOnlyList<IHazardSource> Sources => _sources;

        public HazardService(IEntityLocator entities, INotifier<BattleMap> battleMap)
        {
            _entities = entities;
            _battleMap = battleMap;
        }

        public void Initialize()
        {
            _entities.EntityRemoved += Release;
            _battleMap.AddObserver(this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            ReleaseAll();
            _sources.Clear();
            _sources.AddRange(battleMap.Layout.Hazards);
            _remainingTime = 0f;
        }

        public void Tick()
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime > 0f) return;
            _remainingTime = UPDATE_INTERVAL;
            RefreshMemberships();
        }

        public void RefreshMemberships()
        {
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out ICombatModifiersFacade combat) ||
                    !entity.TryGetFacade(out IUnitTypeFacade type) ||
                    !(type.UnitTypeId.IsShip || type.UnitTypeId.IsSquadron)) continue;
                // The animated hyperspace approach can cross hazards before reaching a clear landing point.
                if (entity.TryGetFacade(out IShipArrivalFacade arrival) && arrival.IsArriving) continue;
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                CombatModifiers modifiers = combat.Modifiers;
                _memberships.TryGetValue(modifiers, out List<IHazardSource> membership);
                foreach (IHazardSource source in _sources)
                {
                    bool wasInside = membership != null && membership.Contains(source);
                    bool isInside = source.Contains(position, 0f, wasInside);
                    if (wasInside == isInside) continue;
                    if (isInside)
                    {
                        if (membership == null)
                        {
                            membership = new List<IHazardSource>();
                            _memberships.Add(modifiers, membership);
                            Action destroyed = () => Release(entity);
                            _destroyHandlers.Add(entity, destroyed);
                            entity.HealthModel.OnDestroy += destroyed;
                        }
                        membership.Add(source);
                        if (source.Data.BlocksSpecialAbilities) modifiers.AddAbilitySuppression();
                        if (source.Data.DisablesShields) modifiers.AddShieldSuppression(source.Data.ShieldRecoveryDelay);
                        Entered?.Invoke(entity, source.Data);
                    }
                    else
                    {
                        membership.Remove(source);
                        RemoveEffects(modifiers, source);
                    }
                }
                if (membership != null && membership.Count == 0) Release(entity);
            }
        }

        public bool BlocksReinforcement(Vector3 position)
        {
            foreach (IHazardSource source in _sources)
                if (source.Data.BlocksReinforcements && source.Contains(position, 0f, false)) return true;
            return false;
        }

        public bool TryGetHazardAt(Vector3 position, out SpaceHazardData data)
        {
            data = null;
            foreach (IHazardSource source in _sources)
            {
                if (!source.Contains(position, 0f, false)) continue;
                if (data == null || source.Data.DisablesShields) data = source.Data;
            }
            return data != null;
        }

        public float GetPathCost(Vector3 position)
        {
            float cost = 0f;
            foreach (IHazardSource source in _sources)
                if (source.Contains(position, 0f, false)) cost += source.Data.PathCost;
            return cost;
        }

        private void Release(IEntity entity)
        {
            if (!_destroyHandlers.TryGetValue(entity, out Action destroyed)) return;
            entity.HealthModel.OnDestroy -= destroyed;
            _destroyHandlers.Remove(entity);
            CombatModifiers modifiers = entity.GetFacade<ICombatModifiersFacade>().Modifiers;
            foreach (IHazardSource source in _memberships[modifiers]) RemoveEffects(modifiers, source);
            _memberships.Remove(modifiers);
        }

        private static void RemoveEffects(CombatModifiers modifiers, IHazardSource source)
        {
            if (source.Data.BlocksSpecialAbilities) modifiers.RemoveAbilitySuppression();
            if (source.Data.DisablesShields) modifiers.RemoveShieldSuppression();
        }

        private void ReleaseAll()
        {
            foreach (IEntity entity in new List<IEntity>(_destroyHandlers.Keys)) Release(entity);
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            _entities.EntityRemoved -= Release;
            ReleaseAll();
            _sources.Clear();
        }
    }
}
