using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Selection;
using UnityEngine;
using EmpireAtWar.Services.Vision;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    public sealed class MiniMapUnitMarkerPresenter : IInitializable, ILateTickable, ILateDisposable
    {
        private readonly IHealthModelObserver _healthModel;
        private readonly IVisionService _visionService;
        private readonly ILocalPlayer _localPlayer;

        private readonly MiniMapData _miniMapData;
        private readonly Transform _viewTransform;
        private MiniMapMarker _marker;

        private readonly PlayerId _owner;
        private readonly SelectionType _selectionType;

        public MiniMapUnitMarkerPresenter(
            IHealthModelObserver healthModel,
            IVisionService visionService,
            ILocalPlayer localPlayer,
            MiniMapData miniMapData,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform,
            PlayerId owner,
            SelectionType selectionType)
        {
            _localPlayer = localPlayer;
            _miniMapData = miniMapData;
            _viewTransform = viewTransform;
            _owner = owner;
            _selectionType = selectionType;
            _healthModel = healthModel;
            _visionService = visionService;
        }

        public void Initialize()
        {
            _healthModel.OnDestroy += RemoveMarker;
            if (_selectionType == SelectionType.Base || _healthModel.IsDestroyed)
            {
                return;
            }

            _marker = new MiniMapMarker(GetMarkType(), _owner);
            Vector3 position = _viewTransform.position;
            _marker.SetPosition(position.x, position.z);
            _marker.SetVisible(_localPlayer.IsFriendly(_owner));
            _miniMapData.AddMarker(_marker);
        }

        public void LateDispose()
        {
            _healthModel.OnDestroy -= RemoveMarker;
            RemoveMarker();
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
                _localPlayer.IsFriendly(_owner) || _visionService.IsVisible(_localPlayer.Id, position));
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
