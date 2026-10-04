using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>
    /// Marks every capture site with its last seen owner; a facility seen built shows its own unit marker instead.
    /// </summary>
    public sealed class CaptureSiteMiniMapPresenter : IInitializable, ILateTickable, ILateDisposable,
        IObserver<BattleState>
    {
        private readonly ICaptureSitesSystem _captureSitesSystem;

        private readonly MiniMapMarkerCollection<ICaptureSite> _markers;
        private readonly INotifier<BattleState> _battleState;

        private bool _hasMarkers;

        public CaptureSiteMiniMapPresenter(
            ICaptureSitesSystem captureSitesSystem,
            MiniMapData miniMapData,
            INotifier<BattleState> battleState)
        {
            _battleState = battleState;
            _markers = new MiniMapMarkerCollection<ICaptureSite>(miniMapData);
            _captureSitesSystem = captureSitesSystem;
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        // Sites are built from the battle map during loading, so they all exist once the battle runs.
        public void UpdateState(BattleState state)
        {
            if (state != BattleState.Running || _hasMarkers)
            {
                return;
            }

            _hasMarkers = true;
            foreach (ICaptureSite site in _captureSitesSystem.Sites)
            {
                MiniMapMarker marker = new MiniMapMarker(MarkType.CaptureSite, site.Owner);
                marker.SetPosition(site.Center.x, site.Center.z);
                marker.SetVisible(true);
                _markers.Add(site, marker);
            }
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _markers.Clear();
        }

        public void LateTick()
        {
            foreach (KeyValuePair<ICaptureSite, MiniMapMarker> pair in _markers.Pairs)
            {
                RefreshMarker(pair.Key, pair.Value);
            }
        }

        private static void RefreshMarker(ICaptureSite site, MiniMapMarker marker)
        {
            if (site.IsRevealed)
            {
                marker.SetOwner(site.Owner);
                marker.SetVisible(!site.IsOperational);
            }
        }
    }
}
