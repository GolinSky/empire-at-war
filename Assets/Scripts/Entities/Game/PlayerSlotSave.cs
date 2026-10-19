using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    [Serializable]
    public sealed class PlayerSlotSave
    {
        [SerializeField] private int index;
        [SerializeField] private int team;
        [SerializeField] private FactionType faction;
        [SerializeField] private PlayerController controller;
        [SerializeField] private EnemyAiDifficulty difficulty;
        [SerializeField] private int colorIndex;

        public static PlayerSlotSave From(PlayerSlot slot) => new PlayerSlotSave
        {
            index = slot.Id.Index,
            team = slot.Team.Value,
            faction = slot.Faction,
            controller = slot.Controller,
            difficulty = slot.Difficulty,
            colorIndex = slot.ColorIndex
        };

        public PlayerSlot ToSlot() =>
            new PlayerSlot(new PlayerId(index), new TeamId(team), faction, controller, difficulty, colorIndex);
    }
}
