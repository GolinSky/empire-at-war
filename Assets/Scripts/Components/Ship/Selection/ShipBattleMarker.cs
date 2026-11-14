using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Extensions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.Player;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Ship.Selection
{
    [LifecycleOrder(Initialize = LifecycleOrders.HUD_PRESENTERS_INITIALIZE)]
    public sealed class ShipBattleMarker : IInitializable, ILateDisposable, IRespawnable, IDespawnable
    {
        private readonly IBattleMarkerLayer _layer;
        private readonly IShipIconProvider _icons;
        private readonly Transform _anchor;
        private readonly IHealthModelObserver _health;
        private readonly IFogVisibilityFacade _fog;
        private readonly ISelectionModelObserver _selection;
        private readonly IPlayerColors _colors;
        private readonly ShipType _shipType;
        private readonly PlayerId _owner;
        private readonly long _entityId;
        private BattleMarker _marker;

        public ShipBattleMarker(IBattleMarkerLayer layer, IShipIconProvider icons,
            [Key(EntityBindType.ViewTransform)] Transform anchor, IHealthModelObserver health,
            IFogVisibilityFacade fog, ISelectionModelObserver selection, IPlayerColors colors,
            ShipType shipType, PlayerId owner, long entityId)
        {
            _layer = layer;
            _icons = icons;
            _anchor = anchor;
            _health = health;
            _fog = fog;
            _selection = selection;
            _colors = colors;
            _shipType = shipType;
            _owner = owner;
            _entityId = entityId;
        }

        public void Initialize()
        {
            if (_health.IsDestroyed)
                return;

            _marker = _layer.CreateShipMarker(_icons.GetShipIcon(_shipType), _anchor);
            _marker.Register(_entityId, _owner);
            UpdateColor(_selection.IsSelected);
            _marker.SetVisible(!_fog.IsHiddenByFog);
            _fog.VisibilityChanged += _marker.SetVisible;
            _selection.OnSelected += UpdateColor;
            _health.OnDestroy += LateDispose;
        }

        public void Respawn() => Initialize();
        public void Despawn() => LateDispose();

        public void LateDispose()
        {
            if (_marker == null)
                return;

            _health.OnDestroy -= LateDispose;
            _selection.OnSelected -= UpdateColor;
            _fog.VisibilityChanged -= _marker.SetVisible;
            _marker.Dispose();
            _marker = null;
        }

        private void UpdateColor(bool selected) => _marker.SetColor(_colors.GetColor(_owner), selected);
    }
}
