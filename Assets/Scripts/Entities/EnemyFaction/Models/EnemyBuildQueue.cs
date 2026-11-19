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
        private readonly List<UnitRequest> _wave = new List<UnitRequest>();
        private readonly HashSet<string> _pipelineIds = new HashSet<string>();
        private readonly HashSet<string> _externalPipelines = new HashSet<string>();
        private readonly Dictionary<string, float> _remainingTime = new Dictionary<string, float>();
        private readonly Dictionary<ShipUnitRequest, float> _blockedSeconds = new Dictionary<ShipUnitRequest, float>();

        public int Count => _builds.Count;
        public IReadOnlyList<EnemyPendingBuild> Builds => _builds;

        public bool CanAdd(string requestId, int maxCount)
        {
            _pipelineIds.Clear();
            foreach (string id in _externalPipelines) _pipelineIds.Add(id);
            int queuedCount = _externalPipelines.Contains(requestId) ? 1 : 0;
            foreach (EnemyPendingBuild build in _builds)
            {
                if (build.TimeLeft > 0f) _pipelineIds.Add(build.Request.Id);
                if (build.Request.Id == requestId) queuedCount++;
            }

            return queuedCount < maxCount &&
                (_pipelineIds.Contains(requestId) || _pipelineIds.Count < PlayerFactionModel.MAX_ACTIVE_PIPELINES);
        }

        public void Add(UnitRequest request, float timeLeft) => _builds.Add(new EnemyPendingBuild(request, timeLeft));

        public void RetryShipDeployment(ShipUnitRequest request, float retryDelay)
        {
            var build = new EnemyPendingBuild(request, 0f) { DeploymentRetryIn = retryDelay };
            _builds.Add(build);
            _blockedSeconds.TryGetValue(request, out float blocked);
            _blockedSeconds[request] = blocked + retryDelay;
        }

        public void RetrySquadronDeployment(SquadronUnitRequest request, float retryDelay) =>
            _builds.Add(new EnemyPendingBuild(request, 0f) { DeploymentRetryIn = retryDelay });

        /// <summary>Seconds a completed ship has been held because its arrival area was blocked.</summary>
        public float GetDeploymentBlockedSeconds(ShipUnitRequest request) =>
            _blockedSeconds.TryGetValue(request, out float blocked) ? blocked : 0f;

        public void ClearDeploymentBlock(ShipUnitRequest request) => _blockedSeconds.Remove(request);

        public int CountHeldShips()
        {
            int count = 0;
            for (int i = 0; i < _builds.Count; i++)
            {
                if (_builds[i].TimeLeft <= 0f && _builds[i].Request is ShipUnitRequest)
                    count++;
            }
            return count;
        }

        public float GetRemainingTime(string requestId)
        {
            float remaining = 0f;
            foreach (EnemyPendingBuild build in _builds)
                if (build.Request.Id == requestId) remaining += build.TimeLeft;
            return remaining;
        }

        public void ReserveExternalPipeline(string requestId) => _externalPipelines.Add(requestId);

        public void ReleaseExternalPipeline(string requestId) => _externalPipelines.Remove(requestId);

        /// <summary>Builds each ID in FIFO order; different IDs advance in parallel.</summary>
        public void Advance(float deltaTime, Action<UnitRequest> complete, int minimumReadyCombatUnits = 0,
            float deploymentWaitLimit = float.PositiveInfinity, bool batchDeploymentRetries = false,
            Action<IReadOnlyList<UnitRequest>> deployWave = null,
            float maxBlockedHoldSeconds = float.PositiveInfinity)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            _finished.Clear();
            _wave.Clear();
            _remainingTime.Clear();
            int readyCombatUnits = 0;
            bool releaseReadyCombat = false;
            foreach (string id in _externalPipelines) _remainingTime.Add(id, 0f);
            for (int i = 0; i < _builds.Count;)
            {
                EnemyPendingBuild build = _builds[i];
                bool isCombatUnit = IsCombatUnit(build.Request);
                float remaining = deltaTime;
                // A completed reinforcement stays paid and reserved without occupying a production lane.
                if (!isCombatUnit || build.TimeLeft > 0f)
                {
                    string id = build.Request.Id;
                    if (_remainingTime.TryGetValue(id, out float pipelineTime))
                    {
                        if (pipelineTime <= 0f)
                        {
                            i++;
                            continue;
                        }
                        remaining = pipelineTime;
                    }
                    // Older saves may contain more pipelines than new purchases allow.
                    else if (_remainingTime.Count >= PlayerFactionModel.MAX_ACTIVE_PIPELINES)
                    {
                        i++;
                        continue;
                    }

                    float elapsed = Math.Min(build.TimeLeft, remaining);
                    build.Advance(elapsed);
                    remaining -= elapsed;
                    _remainingTime[id] = remaining;
                }

                if (build.TimeLeft > 0f)
                {
                    i++;
                    continue;
                }
                float retryElapsed = Math.Min(remaining, build.DeploymentRetryIn);
                build.DeploymentRetryIn -= retryElapsed;
                remaining -= retryElapsed;
                if (build.DeploymentRetryIn > 0f)
                {
                    i++;
                    continue;
                }
                if (minimumReadyCombatUnits > 0 && isCombatUnit &&
                    (batchDeploymentRetries || !(build.Request is ShipUnitRequest ship && _blockedSeconds.ContainsKey(ship))))
                {
                    build.WaitForDeployment(remaining);
                    readyCombatUnits++;
                    releaseReadyCombat |= build.DeploymentWait >= deploymentWaitLimit ||
                        build.Request is ShipUnitRequest blockedShip &&
                        _blockedSeconds.TryGetValue(blockedShip, out float blockedSeconds) &&
                        blockedSeconds >= maxBlockedHoldSeconds;
                    i++;
                    continue;
                }

                _finished.Add(build.Request);
                _builds.RemoveAt(i);
            }

            if (readyCombatUnits > 0 && (readyCombatUnits >= minimumReadyCombatUnits || releaseReadyCombat))
            {
                for (int i = 0; i < _builds.Count;)
                {
                    EnemyPendingBuild build = _builds[i];
                    if (IsCombatUnit(build.Request) && build.IsReadyForDeployment)
                    {
                        (deployWave == null ? _finished : _wave).Add(build.Request);
                        _builds.RemoveAt(i);
                    }
                    else i++;
                }
            }

            for (int i = 0; i < _finished.Count; i++) complete(_finished[i]);
            if (_wave.Count > 0) deployWave(_wave);
        }

        private static bool IsCombatUnit(UnitRequest request) =>
            request is ShipUnitRequest or SquadronUnitRequest;

        public void RemoveAll(Predicate<UnitRequest> match) => _builds.RemoveAll(build => match(build.Request));

        public void Clear()
        {
            _builds.Clear();
            _blockedSeconds.Clear();
            _externalPipelines.Clear();
        }
    }
}
