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
    }
}
