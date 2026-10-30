using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Squadrons;
using UnityEngine;
using Unity.Profiling;

namespace EmpireAtWar.Entities.Capture
{
    public sealed class CaptureStrengthSampler
    {
        private const float REFRESH_INTERVAL = 0.25f;
        private const int MAX_REFRESHES_PER_FRAME = 2;
        private const int MAX_VISITS_PER_FRAME = 4;
        private static readonly ProfilerMarker ShipsMarker = new ProfilerMarker("Battle.Capture.Strength.Ships");
        private static readonly ProfilerMarker SquadronsMarker = new ProfilerMarker("Battle.Capture.Strength.Squadrons");
        private static readonly ProfilerMarker BuildMarker = new ProfilerMarker("Battle.Capture.Strength.Build");
        private readonly IShipService _ships;
        private readonly ISquadronRegistry _squadrons;
        private readonly CaptureStrengthBuilder _builder;
        private readonly float _squadronWeight;
        private readonly List<(Func<Vector3, bool> Contains, Action Prepare)> _areas =
            new List<(Func<Vector3, bool> Contains, Action Prepare)>();
        private readonly List<CaptureStrength> _strengths = new List<CaptureStrength>();
        private readonly List<float> _nextRefresh = new List<float>();
        private float _time;
        private int _cursor;

        public CaptureStrength this[int index] => _strengths[index];

        public CaptureStrengthSampler(IShipService ships, ISquadronRegistry squadrons,
            IPlayerRoster relations, float squadronWeight)
        {
            _ships = ships;
            _squadrons = squadrons;
            _builder = new CaptureStrengthBuilder(relations);
            _squadronWeight = squadronWeight;
        }

        public void Add(Func<Vector3, bool> contains, Action prepare = null)
        {
            _areas.Add((contains, prepare));
            _strengths.Add(default);
            _nextRefresh.Add(0f);
        }

        public void Clear()
        {
            _areas.Clear();
            _strengths.Clear();
            _nextRefresh.Clear();
            _cursor = 0;
        }

        public void Invalidate()
        {
            for (int i = 0; i < _nextRefresh.Count; i++) _nextRefresh[i] = 0f;
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            int visits = Mathf.Min(_areas.Count, MAX_VISITS_PER_FRAME);
            int refreshed = 0;
            for (int visit = 0; visit < visits && refreshed < MAX_REFRESHES_PER_FRAME; visit++)
            {
                if (_cursor >= _areas.Count) _cursor = 0;
                int index = _cursor++;
                if (_time < _nextRefresh[index]) continue;
                _nextRefresh[index] = _time + REFRESH_INTERVAL;
                refreshed++;
                var area = _areas[index];
                if (area.Prepare != null) area.Prepare();
                _builder.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (ShipsMarker.Auto())
#endif
                _ships.AddShipStrength(area.Contains, _builder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (SquadronsMarker.Auto())
#endif
                _squadrons.AddSquadronStrength(area.Contains, _squadronWeight, _builder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BuildMarker.Auto())
#endif
                _strengths[index] = _builder.Build();
            }
        }
    }
}
