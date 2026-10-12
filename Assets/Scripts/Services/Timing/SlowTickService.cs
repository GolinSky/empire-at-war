using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Timing
{
    /// <summary>
    /// Ticks every registered <see cref="ISlowTickable"/> once per <see cref="INTERVAL"/>, spread across frames so
    /// the work never lands in one frame.
    /// </summary>
    public sealed class SlowTickService : ISlowTickService, ITickable
    {
        public const float INTERVAL = 1f;

        private readonly List<ISlowTickable> _tickables = new List<ISlowTickable>();
        private int _cursor;
        private float _owed;

        public void Add(ISlowTickable tickable) => _tickables.Add(tickable);

        public void Remove(ISlowTickable tickable)
        {
            int index = _tickables.IndexOf(tickable);
            if (index < 0)
            {
                return;
            }

            _tickables.RemoveAt(index);
            if (index < _cursor)
            {
                _cursor--;
            }
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            _owed += _tickables.Count * deltaTime / INTERVAL;
            int count = (int)_owed;
            // A long frame ticks everything once instead of carrying the backlog into later frames.
            if (count >= _tickables.Count)
            {
                count = _tickables.Count;
                _owed = 0f;
            }
            else
            {
                _owed -= count;
            }

            for (int i = 0; i < count && _tickables.Count > 0; i++)
            {
                if (_cursor >= _tickables.Count)
                {
                    _cursor = 0;
                }

                _tickables[_cursor++].SlowTick();
            }
        }
    }
}
