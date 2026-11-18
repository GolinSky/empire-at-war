using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Selection;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    /// <summary>
    /// Squadron selection without a world ring: the squadron icon shows the selected state.
    /// Only the local player's own squadrons become selected.
    /// </summary>
    public sealed class SquadronSelectionFacade : IEntitySelectionFacade, IEntitySelectionObserverFacade,
        ISelectionPositionProvider, IRespawnable
    {
        private readonly SelectionModel _model;
        private readonly ISquadronFlightComponent _flight;
        private readonly PlayerId _owner;
        private readonly ILocalPlayer _localPlayer;

        public ISelectionModelObserver Selection => _model;
        public SelectionType SelectionType { get; set; }
        public Vector3 WorldPosition => _flight.Centroid;

        public SquadronSelectionFacade(SelectionModel model, ISquadronFlightComponent flight, PlayerId owner,
            ILocalPlayer localPlayer, SelectionType selectionType)
        {
            _model = model;
            _flight = flight;
            _owner = owner;
            _localPlayer = localPlayer;
            SelectionType = selectionType;
        }

        public void Select(bool isSelected)
        {
            if (!_localPlayer.IsLocal(_owner)) return;
            _model.IsSelected = isSelected;
        }

        public void Respawn() => _model.IsSelected = false;
    }
}
