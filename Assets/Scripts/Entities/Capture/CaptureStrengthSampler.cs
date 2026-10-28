using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Entities.Capture
{
    public sealed class CaptureStrengthSampler
    {
        private const float REFRESH_INTERVAL = 0.25f;
        private readonly IShipService _ships;
        private readonly ISquadronRegistry _squadrons;
        private readonly CaptureStrengthBuilder _builder;
        private readonly float _squadronWeight;
        private readonly List<Func<Vector3, bool>> _areas = new List<Func<Vector3, bool>>();
        private readonly List<CaptureStrength> _strengths = new List<CaptureStrength>();
        private float _countdown;

        public CaptureStrength this[int index] => _strengths[index];

        public CaptureStrengthSampler(IShipService ships, ISquadronRegistry squadrons,
            IPlayerRoster relations, float squadronWeight)
        {
            _ships = ships;
            _squadrons = squadrons;
            _builder = new CaptureStrengthBuilder(relations);
            _squadronWeight = squadronWeight;
        }

        public void Add(Func<Vector3, bool> contains)
        {
            _areas.Add(contains);
            _strengths.Add(default);
            Invalidate();
        }

        public void Clear()
        {
            _areas.Clear();
            _strengths.Clear();
            Invalidate();
        }

        public void Invalidate() => _countdown = 0f;

        public void Tick(float deltaTime)
        {
            _countdown -= deltaTime;
            if (_countdown > 0f) return;
            _countdown = REFRESH_INTERVAL;
            for (int i = 0; i < _areas.Count; i++)
            {
                _builder.Clear();
                _ships.AddShipStrength(_areas[i], _builder);
                _squadrons.AddSquadronStrength(_areas[i], _squadronWeight, _builder);
                _strengths[i] = _builder.Build();
            }
        }
    }
}
