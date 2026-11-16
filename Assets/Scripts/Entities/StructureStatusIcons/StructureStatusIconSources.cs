using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    public sealed class StructureStatusIconSources : IInitializable, ILateDisposable
    {
        private readonly IReadOnlyList<ICaptureSiteStatusEvents> _sites;
        private readonly IRelayStatusEvents _relays;
        private readonly ILocalPlayer _localPlayer;
        private readonly Dictionary<object, int> _indices = new Dictionary<object, int>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<int> _revisions = new List<int>();
        public StructureStatusIconModel Model { get; } = new StructureStatusIconModel();
        public int Version { get; private set; }
        public int Count => _positions.Count;
        public Vector3 GetPosition(int source) => _positions[source];
        public int GetRevision(int source) => _revisions[source];

        public StructureStatusIconSources(IReadOnlyList<ICaptureSiteStatusEvents> sites,
            IRelayStatusEvents relays, ILocalPlayer localPlayer)
        {
            _sites = sites;
            _relays = relays;
            _localPlayer = localPlayer;
        }

        public void Initialize()
        {
            foreach (ICaptureSiteStatusEvents source in _sites)
            {
                source.SitesChanged += Rebuild;
                source.StatusChanged += OnSiteChanged;
            }
            _relays.RelaySourcesChanged += Rebuild;
            _relays.RelayOwnershipChanged += OnRelayChanged;
            Rebuild();
        }

        private void Rebuild()
        {
            _indices.Clear();
            _positions.Clear();
            _revisions.Clear();
            foreach (ICaptureSiteStatusEvents source in _sites)
                foreach (ICaptureSite site in source.Sites) Add(site, site.Center);
            foreach (var relay in _relays.Zones)
                if (relay.IsCapturable) Add(relay, relay.Center);
            Model.Reset(Count);
            foreach (ICaptureSiteStatusEvents source in _sites)
                foreach (ICaptureSite site in source.Sites) Restore(site);
            Version++;
        }

        private void Add(object identity, Vector3 position)
        {
            _indices.Add(identity, _positions.Count);
            _positions.Add(position);
            _revisions.Add(0);
        }

        private void Restore(ICaptureSite site)
        {
            int index = _indices[site];
            Model.Clear(index);
            if (_localPlayer.IsLocal(site.Owner) && !site.IsOperational)
                Model.RestoreCaptured(index, Kind(site));
        }

        private static StructureStatusIconKind Kind(ICaptureSite site) =>
            site.Kind == CaptureSiteKind.Mining ? StructureStatusIconKind.Mining : StructureStatusIconKind.Satellite;

        private void OnSiteChanged(CaptureSiteStatusChange change)
        {
            int index = _indices[change.Site];
            _revisions[index]++;
            _positions[index] = change.Position;
            switch (change.Transition)
            {
                case CaptureSiteStatusTransition.Baseline:
                    Restore(change.Site);
                    break;
                case CaptureSiteStatusTransition.Captured:
                case CaptureSiteStatusTransition.Built:
                    if (_localPlayer.IsLocal(change.Owner))
                        Model.Show(index, Kind(change.Site), change.Transition == CaptureSiteStatusTransition.Captured
                            ? StructureStatusIconState.Captured : StructureStatusIconState.Built);
                    else Model.Clear(index);
                    break;
                case CaptureSiteStatusTransition.Destroyed:
                    if (_localPlayer.IsLocal(change.PreviousOwner))
                        Model.Show(index, Kind(change.Site), StructureStatusIconState.Destroyed);
                    else Model.Clear(index);
                    break;
                case CaptureSiteStatusTransition.OwnershipLost:
                    Model.Clear(index);
                    break;
            }
        }

        private void OnRelayChanged(RelayOwnershipChange change)
        {
            int index = _indices[change.Zone];
            _revisions[index]++;
            _positions[index] = change.Position;
            if (_localPlayer.IsLocal(change.Owner))
                Model.Show(index, StructureStatusIconKind.Relay, StructureStatusIconState.Captured);
            else Model.Clear(index);
        }

        public void LateDispose()
        {
            foreach (ICaptureSiteStatusEvents source in _sites)
            {
                source.SitesChanged -= Rebuild;
                source.StatusChanged -= OnSiteChanged;
            }
            _relays.RelaySourcesChanged -= Rebuild;
            _relays.RelayOwnershipChanged -= OnRelayChanged;
            _indices.Clear();
            _positions.Clear();
            _revisions.Clear();
            Model.Reset(0);
            Version++;
        }
    }
}
