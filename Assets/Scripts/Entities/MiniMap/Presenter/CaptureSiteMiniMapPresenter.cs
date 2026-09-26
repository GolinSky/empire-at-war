using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>Marks revealed capture sites by owner; a built facility shows its own unit marker instead.</summary>
    public sealed class CaptureSiteMiniMapPresenter : IInitializable, ILateTickable, ILateDisposable
    {
        private readonly MiniMapData _miniMapData;
        private readonly CaptureSitesSystem _captureSitesSystem;
        private readonly Dictionary<CaptureSitePresenter, MiniMapMarker> _markers =
            new Dictionary<CaptureSitePresenter, MiniMapMarker>();

        public CaptureSiteMiniMapPresenter(MiniMapData miniMapData, CaptureSitesSystem captureSitesSystem)
        {
            _miniMapData = miniMapData;
            _captureSitesSystem = captureSitesSystem;
        }

        public void Initialize()
        {
            foreach (CaptureSitePresenter site in _captureSitesSystem.Sites)
            {
                MiniMapMarker marker = new MiniMapMarker(MarkType.CaptureSite, site.Owner);
                _markers.Add(site, marker);
                RefreshMarker(site, marker);
                _miniMapData.AddMarker(marker);
            }
        }

        public void LateTick()
        {
            foreach (KeyValuePair<CaptureSitePresenter, MiniMapMarker> pair in _markers)
            {
                RefreshMarker(pair.Key, pair.Value);
            }
        }

        public void LateDispose()
        {
            foreach (MiniMapMarker marker in _markers.Values)
            {
                _miniMapData.RemoveMarker(marker);
            }

            _markers.Clear();
        }

        private static void RefreshMarker(CaptureSitePresenter site, MiniMapMarker marker)
        {
            marker.SetPosition(site.Center.x, site.Center.z);
            marker.SetRelation(site.Owner);
            marker.SetVisible(site.IsRevealed && !site.IsOperational);
        }
    }
}
