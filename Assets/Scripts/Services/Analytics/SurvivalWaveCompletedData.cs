namespace EmpireAtWar.Services.Analytics
{
    public sealed class SurvivalWaveCompletedData
    {
        public string MatchId { get; }
        public int WaveIndex { get; }
        /// <summary>Battle seconds elapsed when the wave was cleared.</summary>
        public float BattleSeconds { get; }
        public int PlayerShipsLeft { get; }

        public SurvivalWaveCompletedData(string matchId, int waveIndex, float battleSeconds, int playerShipsLeft)
        {
            MatchId = matchId;
            WaveIndex = waveIndex;
            BattleSeconds = battleSeconds;
            PlayerShipsLeft = playerShipsLeft;
        }
    }
}
