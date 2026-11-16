using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class ControlGroupSave
    {
        [SerializeField] private int slot;
        [SerializeField] private int owner;
        [SerializeField] private long[] members = Array.Empty<long>();

        public int Slot => slot;
        public PlayerId Owner => new PlayerId(owner);
        public long[] Members => members;

        public static ControlGroupSave Create(int slot, PlayerId owner, long[] members) =>
            new ControlGroupSave { slot = slot, owner = owner.Index, members = members };
    }
}
