using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>
    /// Marks every capture site with its last seen owner; a facility seen built shows its own unit marker instead.
    /// </summary>
    public sealed class CaptureSiteMiniMapPresenter : IInitializable, ILateTickable, ILateDisposable
    {
        private readonly ICaptureSitesSystem _captureSitesSystem;
        private readonly MiniMapMarkerCollection<ICaptureSite> _markers;

        public CaptureSiteMiniMapPresenter(
            MiniMapData miniMapData,
            ICaptureSitesSystem captureSitesSystem)
        {
            _markers = new MiniMapMarkerCollection<ICaptureSite>(miniMapData);
            _captureSitesSystem = captureSitesSystem;
        }

        public void Initialize()
        {
            foreach (ICaptureSite site in _captureSitesSystem.Sites)
            {
                MiniMapMarker marker = new MiniMapMarker(MarkType.CaptureSite, site.Owner);
                marker.SetPosition(site.Center.x, site.Center.z);
                marker.SetVisible(true);
                _markers.Add(site, marker);
            }
        }

        public void LateTick()
        {
            foreach (KeyValuePair<ICaptureSite, MiniMapMarker> pair in _markers.Pairs)
            {
                RefreshMarker(pair.Key, pair.Value);
            }
        }

        public void LateDispose()
        {
            _markers.Clear();
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
