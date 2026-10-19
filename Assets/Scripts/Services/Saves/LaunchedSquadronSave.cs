using System;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>A squadron a hangar bay launched; losing it frees the bay for a replacement.</summary>
    [Serializable]
    public sealed class LaunchedSquadronSave
    {
        [SerializeField] private long squadronId;
        [SerializeField] private int bay;

        public long SquadronId => squadronId;
        public int Bay => bay;

        public static LaunchedSquadronSave Create(long squadronId, int bay) =>
            new LaunchedSquadronSave { squadronId = squadronId, bay = bay };
    }
}
