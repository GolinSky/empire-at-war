using System.Collections.Generic;

namespace EmpireAtWar.Models.Players
{
    /// <summary>Accumulates unit strength inside one capture circle; reuse it with <see cref="Clear"/> each tick.</summary>
    public sealed class CaptureStrengthBuilder
    {
        private readonly IPlayerRoster _playerRoster;

        private readonly float[] _playerStrengths = new float[MatchRules.MAX_PLAYERS];
        private readonly Dictionary<TeamId, float> _teamStrengths = new Dictionary<TeamId, float>();

        public CaptureStrengthBuilder(IPlayerRoster playerRoster)
        {
            _playerRoster = playerRoster;
        }

        public void Clear()
        {
            for (int i = 0; i < _playerStrengths.Length; i++)
            {
                _playerStrengths[i] = 0f;
            }
        }

        public void Add(PlayerId owner, float strength)
        {
            if (owner.IsNone)
            {
                return;
            }

            _playerStrengths[owner.Index] += strength;
        }

        public CaptureStrength Build()
        {
            _teamStrengths.Clear();
            foreach (PlayerSlot slot in _playerRoster.Players)
            {
                float strength = _playerStrengths[slot.Id.Index];
                if (strength <= 0f)
                {
                    continue;
                }

                _teamStrengths.TryGetValue(slot.Team, out float teamStrength);
                _teamStrengths[slot.Team] = teamStrength + strength;
            }

            TeamId leadingTeam = default;
            float leadingStrength = 0f;
            float runnerUpStrength = 0f;
            foreach (KeyValuePair<TeamId, float> team in _teamStrengths)
            {
                if (team.Value > leadingStrength)
                {
                    runnerUpStrength = leadingStrength;
                    leadingStrength = team.Value;
                    leadingTeam = team.Key;
                }
                else if (team.Value > runnerUpStrength)
                {
                    runnerUpStrength = team.Value;
                }
            }

            return new CaptureStrength(
                FindStrongestPlayer(leadingTeam),
                leadingStrength - runnerUpStrength,
                _teamStrengths.Count);
        }

        private PlayerId FindStrongestPlayer(TeamId team)
        {
            PlayerId strongest = PlayerId.None;
            float strongestStrength = 0f;
            foreach (PlayerSlot slot in _playerRoster.Players)
            {
                float strength = _playerStrengths[slot.Id.Index];
                if (slot.Team == team && strength > strongestStrength)
                {
                    strongest = slot.Id;
                    strongestStrength = strength;
                }
            }

            return strongest;
        }
    }
}
