using System;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    public sealed class MiniMapUnitMarkerPresenter : IInitializable, ILateTickable, ILateDisposable
    {
        private readonly MiniMapData _miniMapData;
        private readonly Transform _viewTransform;
        private readonly PlayerType _playerType;
        private readonly SelectionType _selectionType;
        private readonly IHealthModelObserver _healthModel;
        private readonly FogOfWarSystem _fogOfWarSystem;
        private MiniMapMarker _marker;

        public MiniMapUnitMarkerPresenter(
            MiniMapData miniMapData,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform,
            PlayerType playerType,
            SelectionType selectionType,
            IHealthModelObserver healthModel,
            FogOfWarSystem fogOfWarSystem)
        {
            _miniMapData = miniMapData;
            _viewTransform = viewTransform;
            _playerType = playerType;
            _selectionType = selectionType;
            _healthModel = healthModel;
            _fogOfWarSystem = fogOfWarSystem;
        }

        public void Initialize()
        {
            _healthModel.OnDestroy += RemoveMarker;
            if (_selectionType == SelectionType.Base || _healthModel.IsDestroyed)
            {
                return;
            }

            _marker = new MiniMapMarker(GetMarkType(), _playerType);
            Vector3 position = _viewTransform.position;
            _marker.SetPosition(position.x, position.z);
            _marker.SetVisible(_playerType != PlayerType.Opponent);
            _miniMapData.AddMarker(_marker);
        }

        public void LateTick()
        {
            if (_marker == null)
            {
                return;
            }

            if (_healthModel.IsDestroyed)
            {
                RemoveMarker();
                return;
            }

            RefreshMarker();
        }

        public void LateDispose()
        {
            _healthModel.OnDestroy -= RemoveMarker;
            RemoveMarker();
        }

        private MarkType GetMarkType()
        {
            return _selectionType switch
            {
                SelectionType.Ship => MarkType.Ship,
                SelectionType.DefendPlatform => MarkType.DefendPlatform,
                SelectionType.MiningFacility => MarkType.MiningFacility,
                _ => throw new InvalidOperationException(
                    $"Selection type {_selectionType} cannot be represented on the minimap."),
            };
        }

        private void RefreshMarker()
        {
            Vector3 position = _viewTransform.position;
            _marker.SetPosition(position.x, position.z);
            _marker.SetVisible(
                _playerType != PlayerType.Opponent || !_fogOfWarSystem.IsHidden(position));
        }

        private void RemoveMarker()
        {
            if (_marker == null)
            {
                return;
            }

            _marker.SetVisible(false);
            _miniMapData.RemoveMarker(_marker);
            _marker = null;
        }
    }
}
