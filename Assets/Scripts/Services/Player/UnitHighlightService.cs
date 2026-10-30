using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    public sealed class UnitHighlightService : IInitializable, ITickable, ILateDisposable
    {
        private static readonly int HOVER_COLOR_ID = Shader.PropertyToID("_UnitOutlineHoverColor");
        private static readonly int SELECTED_COLOR_ID = Shader.PropertyToID("_UnitOutlineSelectedColor");
        private static readonly int ATTACK_COLOR_ID = Shader.PropertyToID("_UnitOutlineAttackColor");
        private static readonly int OUTLINE_WIDTH_ID = Shader.PropertyToID("_UnitOutlineWidth");

        private readonly UnitHighlightSettings _settings;
        private readonly IUnitOrderService _orders;
        private readonly ILocalPlayer _localPlayer;
        private readonly IEntityLocator _entities;
        private readonly List<(IEntity Entity, IEntityHighlightFacade Facade, float ExpiresAt)> _targets = new();

        public UnitHighlightService(UnitHighlightSettings settings, IUnitOrderService orders,
            ILocalPlayer localPlayer, IEntityLocator entities)
        {
            _settings = settings;
            _orders = orders;
            _localPlayer = localPlayer;
            _entities = entities;
        }

        public void Initialize()
        {
            Shader.SetGlobalVector(HOVER_COLOR_ID, _settings.HoverColor.linear);
            Shader.SetGlobalVector(SELECTED_COLOR_ID, _settings.SelectedColor.linear);
            Shader.SetGlobalVector(ATTACK_COLOR_ID, _settings.AttackColor.linear);
            Shader.SetGlobalFloat(OUTLINE_WIDTH_ID, _settings.OutlineWidth);
            _orders.OrderIssued += HandleOrder;
            _entities.EntityRemoved += HandleEntityRemoved;
        }

        public void Tick()
        {
            float now = Time.time;
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                var target = _targets[i];
                if (now >= target.ExpiresAt || target.Entity.HealthModel.IsDestroyed ||
                    !target.Entity.HealthModel.HasUnits || target.Entity.IsHiddenByFog())
                    ClearAt(i);
            }
        }

        public void LateDispose()
        {
            _orders.OrderIssued -= HandleOrder;
            _entities.EntityRemoved -= HandleEntityRemoved;
            for (int i = _targets.Count - 1; i >= 0; i--)
                ClearAt(i);
        }

        private void HandleOrder(UnitOrder order)
        {
            if (order.Action != UnitActionId.Attack || !_localPlayer.IsLocal(order.Issuer) ||
                order.Target == null || !_localPlayer.IsHostile(order.Target.Owner) ||
                order.Target.HealthModel.IsDestroyed || !order.Target.HealthModel.HasUnits ||
                order.Target.IsHiddenByFog() ||
                !_entities.TryGetEntity(order.Target.Id, out IEntity current) ||
                !ReferenceEquals(current, order.Target) ||
                !order.Target.TryGetFacade(out IEntityHighlightFacade facade))
                return;

            float expiresAt = Time.time + _settings.AttackDuration;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!ReferenceEquals(_targets[i].Entity, order.Target)) continue;
                _targets[i] = (order.Target, facade, expiresAt);
                return;
            }

            facade.SetAttackTargeted(true);
            _targets.Add((order.Target, facade, expiresAt));
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            for (int i = _targets.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_targets[i].Entity, entity))
                    ClearAt(i);
        }

        private void ClearAt(int index)
        {
            _targets[index].Facade.SetAttackTargeted(false);
            _targets.RemoveAt(index);
        }
    }
}
