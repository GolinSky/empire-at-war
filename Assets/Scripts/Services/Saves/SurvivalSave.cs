using System;
using EmpireAtWar.Entities.Game;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Survival progress: the wave and its phase, plus the wave director's queue.</summary>
    [Serializable]
    public sealed class SurvivalSave
    {
        [SerializeField] private int waveIndex;
        [SerializeField] private SurvivalPhase phase;
        [SerializeField] private float preparationTimeLeft;
        [SerializeField] private int directorWave;
        [SerializeField] private UnitTypeSave[] pending = Array.Empty<UnitTypeSave>();
        [SerializeField] private int spawned;
        [SerializeField] private float settleTime;

        public int WaveIndex => waveIndex;
        public SurvivalPhase Phase => phase;
        public float PreparationTimeLeft => preparationTimeLeft;
        public int DirectorWave => directorWave;
        public UnitTypeSave[] Pending => pending;
        public int Spawned => spawned;
        public float SettleTime => settleTime;

        public static SurvivalSave Create(int waveIndex, SurvivalPhase phase, float preparationTimeLeft,
            int directorWave, UnitTypeSave[] pending, int spawned, float settleTime) => new SurvivalSave
        {
            waveIndex = waveIndex, phase = phase, preparationTimeLeft = preparationTimeLeft,
            directorWave = directorWave, pending = pending, spawned = spawned, settleTime = settleTime
        };
    }
}
