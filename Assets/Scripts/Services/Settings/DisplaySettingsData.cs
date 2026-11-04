using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    [Serializable]
    public sealed class DisplaySettingsData
    {
        [SerializeField] private string windowMode = nameof(DisplayWindowMode.Borderless);

        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private uint refreshNumerator;
        [SerializeField] private uint refreshDenominator;
        [SerializeField] private string monitorName = string.Empty;
        [SerializeField] private int monitorIndex;

        public DisplayWindowMode WindowMode
        {
            get => (DisplayWindowMode)Enum.Parse(typeof(DisplayWindowMode), windowMode);
            set => windowMode = value.ToString();
        }

        /// <summary>Zero width and height mean the desktop resolution.</summary>
        public int Width => width;
        public int Height => height;
        public bool UsesDesktopResolution => width <= 0 || height <= 0;
        public uint RefreshNumerator => refreshNumerator;
        public uint RefreshDenominator => refreshDenominator;
        public string MonitorName => monitorName;
        public int MonitorIndex => monitorIndex;

        public void SetRefreshRate(uint numerator, uint denominator)
        {
            refreshNumerator = numerator;
            refreshDenominator = denominator;
        }

        public void SetMonitor(string name, int index)
        {
            monitorName = name;
            monitorIndex = index;
        }

        public void SetResolution(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
        }

        public bool Matches(DisplaySettingsData other)
        {
            return windowMode == other.windowMode && width == other.width && height == other.height
                && refreshNumerator == other.refreshNumerator && refreshDenominator == other.refreshDenominator
                && monitorName == other.monitorName && monitorIndex == other.monitorIndex;
        }

        public DisplaySettingsData Clone()
        {
            return (DisplaySettingsData)MemberwiseClone();
        }

        public void Sanitize()
        {
            if (!Enum.TryParse(windowMode, out DisplayWindowMode mode) || !Enum.IsDefined(typeof(DisplayWindowMode), mode))
            {
                windowMode = nameof(DisplayWindowMode.Borderless);
            }

            if (width <= 0 || height <= 0)
            {
                width = 0;
                height = 0;
            }

            if (refreshNumerator == 0 || refreshDenominator == 0)
            {
                refreshNumerator = 0;
                refreshDenominator = 0;
            }

            if (string.IsNullOrEmpty(monitorName)) monitorName = string.Empty;
            if (monitorIndex < 0 || monitorName.Length == 0) monitorIndex = 0;
        }

        public void Sanitize(IReadOnlyList<DisplayWindowMode> supportedModes)
        {
            Sanitize();
            for (int index = 0; index < supportedModes.Count; index++)
                if (supportedModes[index] == WindowMode) return;
            WindowMode = DisplayWindowMode.Borderless;
        }
    }
}
