using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>The AI's paid builds, using the player's per-ID production pipelines.</summary>
    public sealed class EnemyBuildQueue
    {
        internal const float MAX_DEPLOYMENT_WAIT_SECONDS = 6f;

        private EnemyPendingBuild _lastShipInGroup;
        private readonly List<EnemyPendingBuild> _builds = new List<EnemyPendingBuild>();
        private readonly List<UnitRequest> _finished = new List<UnitRequest>();
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
            // Once the area opens, the already-held ship can depart with any other ships ready to deploy.
            build.WaitForDeployment(float.PositiveInfinity);
            _builds.Add(build);
            _blockedSeconds.TryGetValue(request, out float blocked);
            _blockedSeconds[request] = blocked + retryDelay;
        }

        /// <summary>Seconds a completed ship has been held because its arrival area was blocked.</summary>
        public float GetDeploymentBlockedSeconds(ShipUnitRequest request) =>
            _blockedSeconds.TryGetValue(request, out float blocked) ? blocked : 0f;

        public void ClearDeploymentBlock(ShipUnitRequest request) => _blockedSeconds.Remove(request);

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
        public void Advance(float deltaTime, Action<UnitRequest> complete, bool gatherShips = false,
            float deploymentWaitLimit = MAX_DEPLOYMENT_WAIT_SECONDS)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            _finished.Clear();
            _remainingTime.Clear();
            foreach (string id in _externalPipelines) _remainingTime.Add(id, 0f);
            for (int i = 0; i < _builds.Count;)
            {
                EnemyPendingBuild build = _builds[i];
                bool isShip = build.Request is ShipUnitRequest;
                float remaining = deltaTime;
                // Completed ships wait for deployment without occupying a production lane.
                if (!isShip || build.TimeLeft > 0f)
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
                if (gatherShips && isShip)
                {
                    build.WaitForDeployment(remaining);
                    i++;
                    continue;
                }

                _finished.Add(build.Request);
                _builds.RemoveAt(i);
            }

            if (gatherShips)
                CollectReadyGroup(deploymentWaitLimit);
            else
                _lastShipInGroup = null;

            for (int i = 0; i < _finished.Count; i++) complete(_finished[i]);
        }

        private void CollectReadyGroup(float deploymentWaitLimit)
        {
            // Include the ships already ordered when the first ship finishes. Later orders form the next group.
            if (_lastShipInGroup == null || !_builds.Contains(_lastShipInGroup))
            {
                int ordered = 0;
                bool hasReadyShip = false;
                EnemyPendingBuild last = null;
                foreach (EnemyPendingBuild build in _builds)
                {
                    if (!(build.Request is ShipUnitRequest)) continue;
                    ordered++;
                    hasReadyShip |= build.IsReadyForDeployment;
                    last = build;
                }
                if (!hasReadyShip || ordered == 1 && last.DeploymentWait < deploymentWaitLimit) return;
                _lastShipInGroup = last;
            }

            int end = _builds.IndexOf(_lastShipInGroup);
            bool allReady = true;
            bool waitExpired = false;
            for (int i = 0; i <= end; i++)
            {
                EnemyPendingBuild build = _builds[i];
                if (!(build.Request is ShipUnitRequest)) continue;
                allReady &= build.IsReadyForDeployment;
                waitExpired |= build.IsReadyForDeployment && build.DeploymentWait >= deploymentWaitLimit;
            }
            if (!allReady && !waitExpired) return;

            for (int i = 0; i <= end;)
            {
                if (_builds[i].Request is ShipUnitRequest && _builds[i].IsReadyForDeployment)
                {
                    _finished.Add(_builds[i].Request);
                    _builds.RemoveAt(i);
                    end--;
                }
                else i++;
            }
            _lastShipInGroup = null;
        }

        public void RemoveAll(Predicate<UnitRequest> match) => _builds.RemoveAll(build => match(build.Request));

        public void Clear()
        {
            _builds.Clear();
            _blockedSeconds.Clear();
            _lastShipInGroup = null;
            _externalPipelines.Clear();
        }
    }
}
