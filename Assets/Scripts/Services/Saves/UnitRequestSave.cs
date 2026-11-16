using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>A saved purchase: its <see cref="UnitRequestKey"/> plus remaining build time.</summary>
    [Serializable]
    public sealed class UnitRequestSave
    {
        [SerializeField] private UnitRequestKind kind;
        [SerializeField] private int key;
        [SerializeField] private int tier;
        [SerializeField] private float timeLeft;

        public UnitRequestKind Kind => kind;
        public int Key => key;
        public int Tier => tier;
        public float TimeLeft => timeLeft;
        public UnitRequestKey RequestKey => new UnitRequestKey(kind, key, tier);

        public static UnitRequestSave From(UnitRequest request, float timeLeft = 0f)
        {
            UnitRequestKey requestKey = UnitRequestKey.From(request);
            return new UnitRequestSave
            {
                kind = requestKey.Kind,
                key = requestKey.Key,
                tier = requestKey.Tier,
                timeLeft = timeLeft
            };
        }

        public UnitRequest ToRequest(IUnitRequestFactory factory, FactionRoster roster, StationLevelData levels) =>
            RequestKey.ToRequest(factory, roster, levels);

        internal UnitRequest RestoreRequest(IUnitRequestFactory factory, FactionRoster roster, StationLevelData levels,
            FactionCatalog factions) =>
            Kind == UnitRequestKind.Ship
                ? RestoreShipRequest(factory, roster, factions, (ShipType)Key)
                : ToRequest(factory, roster, levels);

        internal static bool CanRestoreShip(FactionRoster roster, ShipType shipType) =>
            roster.Ships.ContainsKey(shipType) || IsLegacyRebelQuasar(roster, shipType);

        internal static ShipUnitRequest RestoreShipRequest(IUnitRequestFactory factory, FactionRoster roster,
            FactionCatalog factions, ShipType shipType) =>
            factory.ConstructUnitRequest(IsLegacyRebelQuasar(roster, shipType)
                ? factions.Get(FactionType.Empire).Ships[shipType]
                : roster.Ships[shipType], shipType);

        // Existing battles may still own Quasars acquired before the move to Empire.
        private static bool IsLegacyRebelQuasar(FactionRoster roster, ShipType shipType) =>
            roster.FactionType == FactionType.Rebellion && shipType == ShipType.QuasarFire;
    }
}
