using System;
using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Bounds periodic order work even when a frame exceeds the normal update interval.</summary>
    public sealed class SquadronOrderScheduler : ITickable
    {
        private const float UPDATE_INTERVAL = 0.15f;
        private const int MAX_VISITS_PER_FRAME = 64;
        private const int MAX_UPDATES_PER_FRAME = 32;
        private readonly List<Entry> _entries = new List<Entry>();
        private float _time;
        private int _cursor;
        private int _registrations;

        public void Register(Action<float> update)
        {
            float phase = (++_registrations * 0.6180339f % 1f) * UPDATE_INTERVAL;
            _entries.Add(new Entry { Update = update, LastUpdate = _time, NextUpdate = _time + phase });
        }

        public void Unregister(Action<float> update)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Update != update) continue;
                _entries.RemoveAt(i);
                if (i < _cursor) _cursor--;
                return;
            }
        }

        public void Tick() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            int visits = Mathf.Min(_entries.Count, MAX_VISITS_PER_FRAME);
            int updates = 0;
            for (int i = 0; i < visits && _entries.Count > 0 && updates < MAX_UPDATES_PER_FRAME; i++)
            {
                if (_cursor >= _entries.Count) _cursor = 0;
                Entry entry = _entries[_cursor++];
                if (_time < entry.NextUpdate) continue;
                float elapsed = _time - entry.LastUpdate;
                entry.LastUpdate = _time;
                entry.NextUpdate = _time + UPDATE_INTERVAL;
                updates++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.SquadronOrder.Auto())
#endif
                entry.Update(elapsed);
            }
        }

        private sealed class Entry
        {
            public Action<float> Update;
            public float LastUpdate;
            public float NextUpdate;
        }
    }
}
