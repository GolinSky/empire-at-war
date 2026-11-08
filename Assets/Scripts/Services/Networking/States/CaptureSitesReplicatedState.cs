using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>Capture-site owner, capture and construction progress, in map layout order (as saves).</summary>
    public sealed class CaptureSitesReplicatedState : ReplicatedState
    {
        private readonly CaptureSitesSystem _sites;

        public CaptureSitesReplicatedState(ReplicatedStateRegistry registry, IMatchSession session,
            CaptureSitesSystem sites) : base(registry, session)
        {
            _sites = sites;
        }

        public override string Key => "capture-sites";

        public override string Capture() => JsonUtility.ToJson(new Snapshot
        {
            Sites = _sites.SitePresenters.Select(site => CaptureSiteSave.Create(site.Owner, site.State,
                site.CapturingPlayer, site.CaptureProgress, site.FacilityType, site.ConstructionProgress,
                site.FacilityId)).ToArray()
        });

        public override void Apply(string json)
        {
            CaptureSiteSave[] saves = JsonUtility.FromJson<Snapshot>(json).Sites;
            IReadOnlyList<CaptureSitePresenter> sites = _sites.SitePresenters;
            if (saves.Length != sites.Count)
                throw new InvalidOperationException($"Host sent {saves.Length} capture sites; the map has {sites.Count}.");
            for (int i = 0; i < sites.Count; i++)
            {
                CaptureSiteSave save = saves[i];
                sites[i].Restore(save.Owner, save.State, save.CapturingPlayer, save.CaptureProgress, save.FacilityType,
                    save.ConstructionProgress);
                sites[i].SetFacility(save.FacilityId);
            }
        }

        protected override void FollowHost() => _sites.FollowHost();

        [Serializable]
        private sealed class Snapshot
        {
            public CaptureSiteSave[] Sites;
        }
    }
}
