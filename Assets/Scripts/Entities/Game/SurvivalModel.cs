using System;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalModel
    {
        public const int WAVE_COUNT = 5;
        private readonly float _betweenWaves;
        public event Action Changed;
        public int WaveIndex { get; private set; } = 1;
        public SurvivalPhase Phase { get; private set; } = SurvivalPhase.Preparation;
        public float PreparationTimeLeft { get; private set; }

        public SurvivalModel(float firstPreparationSeconds = 120f, float betweenWavesSeconds = 60f)
        {
            PreparationTimeLeft = firstPreparationSeconds;
            _betweenWaves = betweenWavesSeconds;
        }

        public void Advance(float seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (Phase != SurvivalPhase.Preparation) return;
            PreparationTimeLeft = Math.Max(0f, PreparationTimeLeft - seconds);
            if (PreparationTimeLeft == 0) Phase = SurvivalPhase.WaveActive;
            Changed?.Invoke();
        }

        public void StartWaveNow()
        {
            if (Phase != SurvivalPhase.Preparation) return;
            Advance(PreparationTimeLeft);
        }

        public void CompleteWave()
        {
            if (Phase != SurvivalPhase.WaveActive) return;
            if (WaveIndex == WAVE_COUNT) Phase = SurvivalPhase.Won;
            else
            {
                WaveIndex++;
                Phase = SurvivalPhase.Preparation;
                PreparationTimeLeft = _betweenWaves;
            }
            Changed?.Invoke();
        }

        public void Restore(int waveIndex, SurvivalPhase phase, float preparationTimeLeft)
        {
            WaveIndex = waveIndex;
            Phase = phase;
            PreparationTimeLeft = preparationTimeLeft;
            Changed?.Invoke();
        }

        public void Lose()
        {
            if (Phase == SurvivalPhase.Won || Phase == SurvivalPhase.Lost) return;
            Phase = SurvivalPhase.Lost;
            Changed?.Invoke();
        }
    }
}
