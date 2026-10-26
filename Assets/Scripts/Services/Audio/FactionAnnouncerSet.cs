using System;
using EmpireAtWar.Utils.Random;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class FactionAnnouncerSet
    {
        [SerializeField] private AudioClip upgradeProgress;
        [SerializeField] private AudioClip upgradeCanceled;
        [SerializeField] private AudioClip techUpgradeProgress;
        [SerializeField] private AudioClip techUpgradeCanceled;
        [SerializeField] private AudioClip stationUpgraded;
        [SerializeField] private AudioClip enemyStationUpgraded;
        [SerializeField] private AudioClip unitCapReached;
        [SerializeField] private AudioClip upgradeComplete;
        [SerializeField] private AudioClip newConstruction;
        [SerializeField] private RandomAudioClips stationUnderAttack;
        [SerializeField] private AudioClip miningUnderAttack;
        [SerializeField] private AudioClip miningDestroyed;
        [SerializeField] private RandomAudioClips miningCaptured;
        [SerializeField] private AudioClip miningLost;
        [SerializeField] private AudioClip reinforcementsEnRoute;
        [SerializeField] private AudioClip unitDestroyed;
        [SerializeField] private AudioClip defeated;
        [SerializeField] private AudioClip enemyFleetApproaching;
        [SerializeField] private AudioClip constructionComplete;
        [SerializeField] private AudioClip unitProduction;
        [SerializeField] private AudioClip buildingStarted;

        public AudioClip UpgradeProgress => upgradeProgress;
        public AudioClip UpgradeCanceled => upgradeCanceled;
        public AudioClip TechUpgradeProgress => techUpgradeProgress;
        public AudioClip TechUpgradeCanceled => techUpgradeCanceled;
        public AudioClip StationUpgraded => stationUpgraded;
        public AudioClip EnemyStationUpgraded => enemyStationUpgraded;
        public AudioClip UnitCapReached => unitCapReached;
        public AudioClip UpgradeComplete => upgradeComplete;
        public AudioClip NewConstruction => newConstruction;
        public AudioClip StationUnderAttack => stationUnderAttack.GetRandom();
        public AudioClip MiningUnderAttack => miningUnderAttack;
        public AudioClip MiningDestroyed => miningDestroyed;
        public AudioClip MiningCaptured => miningCaptured.GetRandom();
        public AudioClip MiningLost => miningLost;
        public AudioClip ReinforcementsEnRoute => reinforcementsEnRoute;
        public AudioClip UnitDestroyed => unitDestroyed;
        public AudioClip Defeated => defeated;
        public AudioClip EnemyFleetApproaching => enemyFleetApproaching;
        public AudioClip ConstructionComplete => constructionComplete;
        public AudioClip UnitProduction => unitProduction;
        public AudioClip BuildingStarted => buildingStarted;
    }
}
