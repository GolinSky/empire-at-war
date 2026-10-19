using System;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>One capture site, matched by its index in the seeded map layout.</summary>
    [Serializable]
    public sealed class CaptureSiteSave
    {
        private const int NO_PLAYER = -1;

        [SerializeField] private int owner = NO_PLAYER;
        [SerializeField] private CaptureSiteState state;
        [SerializeField] private int capturingPlayer = NO_PLAYER;
        [SerializeField] private float captureProgress;
        [SerializeField] private SiteFacilityType facilityType;
        [SerializeField] private float constructionProgress;
        [SerializeField] private long facilityId;

        public PlayerId Owner => ToPlayer(owner);
        public CaptureSiteState State => state;
        public PlayerId CapturingPlayer => ToPlayer(capturingPlayer);
        public float CaptureProgress => captureProgress;
        public SiteFacilityType FacilityType => facilityType;
        public float ConstructionProgress => constructionProgress;
        /// <summary>The operational facility's entity id; only meaningful when <see cref="State"/> is Operational.</summary>
        public long FacilityId => facilityId;

        public static CaptureSiteSave Create(PlayerId owner, CaptureSiteState state, PlayerId capturingPlayer,
            float captureProgress, SiteFacilityType facilityType, float constructionProgress, long facilityId) =>
            new CaptureSiteSave
            {
                owner = FromPlayer(owner), state = state, capturingPlayer = FromPlayer(capturingPlayer),
                captureProgress = captureProgress, facilityType = facilityType,
                constructionProgress = constructionProgress, facilityId = facilityId
            };

        private static int FromPlayer(PlayerId player) => player.IsNone ? NO_PLAYER : player.Index;

        private static PlayerId ToPlayer(int index) => index == NO_PLAYER ? PlayerId.None : new PlayerId(index);
    }
}
