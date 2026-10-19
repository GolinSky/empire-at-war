using System;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Bay reserves and the launch timer; disabled bays follow from the restored hangar hardpoints.</summary>
    [Serializable]
    public sealed class HangarSave
    {
        [SerializeField] private int[] reserve = Array.Empty<int>();
        [SerializeField] private float cooldown;
        [SerializeField] private LaunchedSquadronSave[] launched = Array.Empty<LaunchedSquadronSave>();

        public int[] Reserve => reserve;
        public float Cooldown => cooldown;
        public LaunchedSquadronSave[] Launched => launched;

        public static HangarSave Create(int[] reserve, float cooldown, LaunchedSquadronSave[] launched) =>
            new HangarSave { reserve = reserve, cooldown = cooldown, launched = launched };
    }
}
