using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>Drop reserve units of one kind at the given slots; blocked slots and missing capacity are skipped.</summary>
    public readonly struct DeployReinforcementCommand
    {
        public PlayerId Issuer { get; }
        public UnitRequestKind Kind { get; }
        public int Key { get; }
        public Vector3[] Slots { get; }

        public DeployReinforcementCommand(PlayerId issuer, UnitRequestKind kind, int key, Vector3[] slots)
        {
            Issuer = issuer;
            Kind = kind;
            Key = key;
            Slots = slots;
        }
    }
}
