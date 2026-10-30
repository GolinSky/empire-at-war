using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>The AI's paid builds, using the player's per-ID production pipelines.</summary>
    public sealed class EnemyBuildQueue
    {
        private readonly List<EnemyPendingBuild> _builds = new List<EnemyPendingBuild>();
        private readonly List<UnitRequest> _finished = new List<UnitRequest>();
        private readonly HashSet<string> _pipelineIds = new HashSet<string>();
        private readonly HashSet<string> _externalPipelines = new HashSet<string>();
        private readonly Dictionary<string, float> _remainingTime = new Dictionary<string, float>();

        public int Count => _builds.Count;
        public IReadOnlyList<EnemyPendingBuild> Builds => _builds;

        public bool CanAdd(string requestId, int maxCount)
        {
            _pipelineIds.Clear();
            foreach (string id in _externalPipelines) _pipelineIds.Add(id);
            int queuedCount = _externalPipelines.Contains(requestId) ? 1 : 0;
            foreach (EnemyPendingBuild build in _builds)
            {
                _pipelineIds.Add(build.Request.Id);
                if (build.Request.Id == requestId) queuedCount++;
            }

            return queuedCount < maxCount &&
                (queuedCount > 0 || _pipelineIds.Count < PlayerFactionModel.MAX_ACTIVE_PIPELINES);
        }

        public void Add(UnitRequest request, float timeLeft) => _builds.Add(new EnemyPendingBuild(request, timeLeft));

        public void ReserveExternalPipeline(string requestId) => _externalPipelines.Add(requestId);

        public void ReleaseExternalPipeline(string requestId) => _externalPipelines.Remove(requestId);

        /// <summary>Builds each ID in FIFO order; different IDs advance in parallel.</summary>
        public void Advance(float deltaTime, Action<UnitRequest> complete)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            _finished.Clear();
            _remainingTime.Clear();
            foreach (string id in _externalPipelines) _remainingTime.Add(id, 0f);
            for (int i = 0; i < _builds.Count;)
            {
                EnemyPendingBuild build = _builds[i];
                string id = build.Request.Id;
                if (_remainingTime.TryGetValue(id, out float remaining))
                {
                    if (remaining <= 0f)
                    {
                        i++;
                        continue;
                    }
                }
                else
                {
                    // Older saves may contain more pipelines than new purchases allow.
                    if (_remainingTime.Count >= PlayerFactionModel.MAX_ACTIVE_PIPELINES)
                    {
                        i++;
                        continue;
                    }
                    remaining = deltaTime;
                }

                float elapsed = Math.Min(build.TimeLeft, remaining);
                build.Advance(elapsed);
                _remainingTime[id] = remaining - elapsed;
                if (build.TimeLeft > 0f)
                {
                    i++;
                    continue;
                }

                _finished.Add(build.Request);
                _builds.RemoveAt(i);
            }

            for (int i = 0; i < _finished.Count; i++) complete(_finished[i]);
        }

        public void RemoveAll(Predicate<UnitRequest> match) => _builds.RemoveAll(build => match(build.Request));

        public void Clear()
        {
            _builds.Clear();
            _externalPipelines.Clear();
        }
    }
}
