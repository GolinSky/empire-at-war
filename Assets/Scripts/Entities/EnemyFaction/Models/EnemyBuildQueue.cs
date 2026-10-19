using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>The AI's paid builds, each counting down its own build time; all build in parallel.</summary>
    public sealed class EnemyBuildQueue
    {
        private readonly List<EnemyPendingBuild> _builds = new List<EnemyPendingBuild>();
        private readonly List<UnitRequest> _finished = new List<UnitRequest>();

        public int Count => _builds.Count;
        public IReadOnlyList<EnemyPendingBuild> Builds => _builds;

        public void Add(UnitRequest request, float timeLeft) => _builds.Add(new EnemyPendingBuild(request, timeLeft));

        /// <summary>Counts every build down; finished builds leave the queue before <paramref name="complete"/> runs for each.</summary>
        public void Advance(float deltaTime, Action<UnitRequest> complete)
        {
            _finished.Clear();
            for (int i = _builds.Count - 1; i >= 0; i--)
            {
                _builds[i].Advance(deltaTime);
                if (_builds[i].TimeLeft > 0f) continue;
                _finished.Add(_builds[i].Request);
                _builds.RemoveAt(i);
            }

            for (int i = _finished.Count - 1; i >= 0; i--) complete(_finished[i]);
        }

        public void Clear() => _builds.Clear();
    }
}
