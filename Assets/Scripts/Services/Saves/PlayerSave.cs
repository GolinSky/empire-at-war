using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// One player's economy and production. Each section is filled by the player-scope source that owns it,
    /// so a feature a player does not have leaves its section empty.
    /// </summary>
    [Serializable]
    public sealed class PlayerSave
    {
        [SerializeField] private int player;
        [SerializeField] private bool hasMoney;
        [SerializeField] private float money;
        [SerializeField] private int stationLevel = 1;
        [SerializeField] private ResearchSave[] research = Array.Empty<ResearchSave>();
        [SerializeField] private UnitRequestSave[] production = Array.Empty<UnitRequestSave>();
        [SerializeField] private UnitRequestSave[] reserve = Array.Empty<UnitRequestSave>();
        [SerializeField] private UnitRequestSave[] pendingBuilds = Array.Empty<UnitRequestSave>();
        [SerializeField] private int shipOrders;
        [SerializeField] private long[] paidSquadrons = Array.Empty<long>();
        [SerializeField] private SuperWeaponSave[] superWeapons = Array.Empty<SuperWeaponSave>();

        public PlayerId Player => new PlayerId(player);
        public bool HasMoney => hasMoney;
        public float Money => money;
        public int StationLevel { get => stationLevel; set => stationLevel = value; }
        public ResearchSave[] Research { get => research; set => research = value; }
        /// <summary>The human's production queue in pipeline order; each head item carries its time left.</summary>
        public UnitRequestSave[] Production { get => production; set => production = value; }
        /// <summary>The human's built units waiting to be placed, one entry per unit.</summary>
        public UnitRequestSave[] Reserve { get => reserve; set => reserve = value; }
        /// <summary>The AI's paid builds with their time left.</summary>
        public UnitRequestSave[] PendingBuilds { get => pendingBuilds; set => pendingBuilds = value; }
        public int ShipOrders { get => shipOrders; set => shipOrders = value; }
        /// <summary>Squadrons the player paid for. Every other squadron came from a hangar bay, even one whose carrier is gone.</summary>
        public long[] PaidSquadrons { get => paidSquadrons; set => paidSquadrons = value; }
        public SuperWeaponSave[] SuperWeapons { get => superWeapons; set => superWeapons = value; }

        public static PlayerSave Create(PlayerId player) => new PlayerSave { player = player.Index };

        public void SetMoney(float value)
        {
            hasMoney = true;
            money = value;
        }
    }
}
