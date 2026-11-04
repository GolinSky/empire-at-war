using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Builds display choices from the monitor currently hosting the window.</summary>
    public sealed class SettingsDisplayEditor
    {
        private readonly IDisplayOptions _options;
        private readonly ISettingsService _settings;
        private readonly SettingsModel _model;
        private readonly List<Vector2Int> _sizes = new();
        private IReadOnlyList<RefreshRate> _rates = Array.Empty<RefreshRate>();
        private string[] _sizeLabels = Array.Empty<string>();
        private string[] _modeLabels = Array.Empty<string>();

        private DisplaySettingsData Draft => _settings.Draft.Display;

        public SettingsDisplayEditor(IDisplayOptions options, ISettingsService settings, SettingsModel model)
        {
            _options = options;
            _settings = settings;
            _model = model;
        }

        public void Open()
        {
            Draft.Sanitize(_options.WindowModes);
            _sizes.Clear();
            _sizes.Add(Vector2Int.zero);
            _sizes.AddRange(_options.Resolutions);
            Vector2Int saved = new Vector2Int(Draft.Width, Draft.Height);
            if (!Draft.UsesDesktopResolution && !_sizes.Contains(saved) &&
                Draft.WindowMode != DisplayWindowMode.ExclusiveFullscreen)
                _sizes.Add(saved);
            _sizeLabels = new string[_sizes.Count];
            _sizeLabels[0] = $"Desktop ({_options.DesktopResolution.x} x {_options.DesktopResolution.y})";
            for (int i = 1; i < _sizes.Count; i++)
                _sizeLabels[i] = $"{_sizes[i].x} x {_sizes[i].y}";
            _modeLabels = new string[_options.WindowModes.Count];
            for (int i = 0; i < _modeLabels.Length; i++)
                _modeLabels[i] = _options.WindowModes[i] switch
                {
                    DisplayWindowMode.ExclusiveFullscreen => "Exclusive Fullscreen",
                    DisplayWindowMode.MaximizedWindow => "Native Fullscreen",
                    _ => _options.WindowModes[i].ToString(),
                };
        }

        public SettingsChoice WindowMode
        {
            get
            {
                int index = 0;
                for (int i = 0; i < _options.WindowModes.Count; i++)
                    if (_options.WindowModes[i] == Draft.WindowMode) index = i;
                return new SettingsChoice(_modeLabels, index, true);
            }
        }

        public SettingsChoice Resolution => new SettingsChoice(_sizeLabels,
            Math.Max(0, _sizes.IndexOf(new Vector2Int(Draft.Width, Draft.Height))), true);

        public void Refresh()
        {
            IReadOnlyList<DisplayInfo> monitors = _options.Monitors;
            string[] labels = new string[monitors.Count];
            int selected = 0;
            for (int i = 0; i < monitors.Count; i++)
            {
                labels[i] = $"{i + 1}: {monitors[i].name}";
                if (monitors[i].Equals(_options.CurrentMonitor)) selected = i;
            }

            bool exclusive = Draft.WindowMode == DisplayWindowMode.ExclusiveFullscreen;
            _rates = _options.RefreshRatesFor(Draft.UsesDesktopResolution
                ? _options.DesktopResolution : new Vector2Int(Draft.Width, Draft.Height));
            string[] rateLabels = exclusive ? new string[_rates.Count + 1] : new[] { FormatRate(_options.CurrentRefreshRate) };
            int rateIndex = 0;
            if (exclusive)
            {
                rateLabels[0] = "Monitor default";
                for (int i = 0; i < _rates.Count; i++)
                {
                    rateLabels[i + 1] = FormatRate(_rates[i]);
                    if (Matches(_rates[i])) rateIndex = i + 1;
                }
            }
            _model.SetDisplayOptions(new SettingsChoice(labels, selected, monitors.Count > 1),
                new SettingsChoice(rateLabels, rateIndex, exclusive && _rates.Count > 0));
        }

        public void SelectWindowMode(int index)
        {
            Draft.WindowMode = _options.WindowModes[index];
            if (Draft.WindowMode == DisplayWindowMode.ExclusiveFullscreen && !Draft.UsesDesktopResolution)
            {
                Vector2Int size = new Vector2Int(Draft.Width, Draft.Height);
                bool supported = false;
                foreach (Vector2Int available in _options.Resolutions)
                    if (available == size) supported = true;
                if (!supported) Draft.SetResolution(0, 0);
            }
            Open();
            NormalizeRefreshRate();
        }

        public void SelectResolution(int index)
        {
            Vector2Int size = _sizes[index];
            Draft.SetResolution(size.x, size.y);
            NormalizeRefreshRate();
        }

        public void SelectRefreshRate(int index)
        {
            if (index == 0) Draft.SetRefreshRate(0, 0);
            else Draft.SetRefreshRate(_rates[index - 1].numerator, _rates[index - 1].denominator);
        }

        private void NormalizeRefreshRate()
        {
            if (Draft.RefreshNumerator == 0) return;
            IReadOnlyList<RefreshRate> rates = _options.RefreshRatesFor(Draft.UsesDesktopResolution
                ? _options.DesktopResolution : new Vector2Int(Draft.Width, Draft.Height));
            foreach (RefreshRate rate in rates)
                if (Matches(rate)) return;
            if (rates.Count == 0) Draft.SetRefreshRate(0, 0);
            else
            {
                RefreshRate highest = rates[0];
                foreach (RefreshRate rate in rates)
                    if (rate.value > highest.value) highest = rate;
                Draft.SetRefreshRate(highest.numerator, highest.denominator);
            }
        }

        private bool Matches(RefreshRate rate) =>
            (ulong)rate.numerator * Draft.RefreshDenominator == (ulong)Draft.RefreshNumerator * rate.denominator;

        public static string FormatRate(RefreshRate rate) =>
            rate.value.ToString("0.##", CultureInfo.InvariantCulture) + " Hz";
    }
}
