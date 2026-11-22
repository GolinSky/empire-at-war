using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// One second of network numbers as ordered named columns. The first window fixes the columns; later windows
    /// must set the same names in the same order so every CSV row matches the header. NaN = no value.
    /// </summary>
    public sealed class NetworkStatsRow
    {
        private readonly List<string> _names = new List<string>();
        private readonly List<double> _values = new List<double>();
        private int _cursor;

        public int Count => _names.Count;

        public void Begin() => _cursor = 0;

        public void Set(string name, double value)
        {
            if (_cursor == _names.Count)
            {
                _names.Add(name);
                _values.Add(value);
            }
            else
            {
                if (_names[_cursor] != name)
                    throw new InvalidOperationException($"Column {_cursor} changed from '{_names[_cursor]}' to '{name}'.");
                _values[_cursor] = value;
            }
            _cursor++;
        }

        /// <summary>Every column must be set each window.</summary>
        public void End()
        {
            if (_cursor != _names.Count)
                throw new InvalidOperationException($"Only {_cursor} of {_names.Count} columns were set.");
        }

        public string GetName(int index) => _names[index];

        public double GetValue(int index) => _values[index];
    }
}
