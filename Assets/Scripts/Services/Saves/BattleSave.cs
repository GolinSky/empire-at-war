using System;
using EmpireAtWar.Entities.Game;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>One save file: the header, the inputs that rebuild the battle, and the restored values.</summary>
    [Serializable]
    public sealed class BattleSave
    {
        [SerializeField] private BattleSaveHeader header;
        [SerializeField] private BattleSetupSave setup;
        [SerializeField] private CameraPoseSave camera;
        [SerializeField] private UnitSave[] units = Array.Empty<UnitSave>();
        [SerializeField] private StructureSave[] structures = Array.Empty<StructureSave>();
        [SerializeField] private PlayerSave[] players = Array.Empty<PlayerSave>();
        [SerializeField] private CaptureSiteSave[] captureSites = Array.Empty<CaptureSiteSave>();
        [SerializeField] private ReinforcementZoneSave[] reinforcementZones = Array.Empty<ReinforcementZoneSave>();
        [SerializeField] private int[] observedBases = Array.Empty<int>();
        [SerializeField] private bool hasSurvival;
        [SerializeField] private SurvivalSave survival;

        public BattleSaveHeader Header => header;
        public BattleSetupSave Setup => setup;
        public CameraPoseSave Camera => camera;
        public UnitSave[] Units => units;
        public StructureSave[] Structures => structures;
        public PlayerSave[] Players { get => players; set => players = value; }
        /// <summary>In map layout order; empty when the battle has no capture sites.</summary>
        public CaptureSiteSave[] CaptureSites { get => captureSites; set => captureSites = value; }
        /// <summary>Relay ownership and capture state, in map layout order.</summary>
        public ReinforcementZoneSave[] ReinforcementZones { get => reinforcementZones; set => reinforcementZones = value; }
        /// <summary>Players whose station the victory rule has seen alive, by player index.</summary>
        public int[] ObservedBases { get => observedBases; set => observedBases = value; }
        /// <summary>Null outside Survival.</summary>
        public SurvivalSave Survival
        {
            get => hasSurvival ? survival : null;
            set
            {
                hasSurvival = true;
                survival = value;
            }
        }

        public static BattleSave Create(BattleSaveHeader header, BattleSetupSave setup, CameraPoseSave camera,
            UnitSave[] units, StructureSave[] structures) =>
            new BattleSave { header = header, setup = setup, camera = camera, units = units, structures = structures };

        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>Throws <see cref="ArgumentException"/> for malformed JSON.</summary>
        public static BattleSave FromJson(string json) => JsonUtility.FromJson<BattleSave>(json);
    }
}
