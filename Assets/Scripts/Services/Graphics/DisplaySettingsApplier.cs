using System.Collections.Generic;
using System.Threading.Tasks;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.Graphics
{
    /// <summary>
    /// Applies monitor placement before querying its modes and changing output size.
    /// </summary>
    public sealed class DisplaySettingsApplier : ISettingsApplier, IDisplayOptions, ITickable
    {
        private bool _windowNeedsCentering;
        public Task PendingApply { get; private set; } = Task.CompletedTask;
        public IReadOnlyList<DisplayWindowMode> WindowModes { get; } =
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            new[] { DisplayWindowMode.Windowed, DisplayWindowMode.Borderless, DisplayWindowMode.MaximizedWindow };
#else
            new[] { DisplayWindowMode.Windowed, DisplayWindowMode.Borderless, DisplayWindowMode.ExclusiveFullscreen };
#endif

        public RefreshRate CurrentRefreshRate => Screen.currentResolution.refreshRateRatio;
        public DisplayInfo CurrentMonitor => Screen.mainWindowDisplayInfo;
        public IReadOnlyList<DisplayInfo> Monitors
        {
            get
            {
                List<DisplayInfo> displays = new List<DisplayInfo>();
                Screen.GetDisplayLayout(displays);
                return displays;
            }
        }

        public IReadOnlyList<Vector2Int> Resolutions
        {
            get
            {
                List<Vector2Int> sizes = new List<Vector2Int>();
                foreach (Resolution resolution in Screen.resolutions)
                {
                    Vector2Int size = new Vector2Int(resolution.width, resolution.height);
                    if (!sizes.Contains(size))
                    {
                        sizes.Add(size);
                    }
                }

                sizes.Sort((a, b) => b.x != a.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
                return sizes;
            }
        }

        public Vector2Int DesktopResolution =>
            new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);

        public IReadOnlyList<RefreshRate> RefreshRatesFor(Vector2Int size)
        {
            List<RefreshRate> rates = new List<RefreshRate>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                RefreshRate rate = resolution.refreshRateRatio;
                if (resolution.width == size.x && resolution.height == size.y && !rates.Contains(rate))
                    rates.Add(rate);
            }
            rates.Sort((a, b) => b.value.CompareTo(a.value));
            return rates;
        }

        public DisplaySettingsData CaptureCurrentDisplay()
        {
            DisplaySettingsData display = new DisplaySettingsData();
            display.WindowMode = Screen.fullScreenMode switch
            {
                FullScreenMode.Windowed => DisplayWindowMode.Windowed,
                FullScreenMode.ExclusiveFullScreen => DisplayWindowMode.ExclusiveFullscreen,
                FullScreenMode.MaximizedWindow => DisplayWindowMode.MaximizedWindow,
                _ => DisplayWindowMode.Borderless
            };
            display.SetResolution(Screen.width, Screen.height);
            display.SetRefreshRate(CurrentRefreshRate.numerator, CurrentRefreshRate.denominator);
            IReadOnlyList<DisplayInfo> monitors = Monitors;
            for (int index = 0; index < monitors.Count; index++)
                if (monitors[index].Equals(CurrentMonitor))
                {
                    display.SetMonitor(monitors[index].name, index);
                    break;
                }
            return display;
        }

        public void Apply(SettingsData settings)
        {
            ApplyDisplayAsync(settings.Display);
        }

        public void Tick()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // Follow the actual mode after deferred display changes and restore confinement after focus changes.
            CursorLockMode lockMode = Application.isFocused &&
                Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen
                ? CursorLockMode.Confined : CursorLockMode.None;
            if (Cursor.lockState != lockMode)
                Cursor.lockState = lockMode;
#endif
        }

        public Task ApplyDisplayAsync(DisplaySettingsData display)
        {
            PendingApply = ApplyQueuedAsync(PendingApply, display, true);
            return PendingApply;
        }

        public Task MoveToMonitorAsync(DisplaySettingsData display)
        {
            PendingApply = ApplyQueuedAsync(PendingApply, display, false);
            return PendingApply;
        }

        private async Task ApplyQueuedAsync(Task previous, DisplaySettingsData display, bool resize)
        {
            try { await previous; }
            catch (System.Exception exception)
            {
                // The previous caller receives its failure; allow a subsequent revert to restore the display.
                Debug.LogWarning($"Retrying display configuration after a failed operation: {exception.Message}");
            }
            display.Sanitize(WindowModes);
            DisplayInfo target = ResolveMonitor(display);
            bool moved = !target.Equals(CurrentMonitor);
            if (moved)
            {
                await MoveAsync(target, display.WindowMode);
                _windowNeedsCentering = true;
            }
            if (!resize) return;

            IReadOnlyList<Vector2Int> sizes = Resolutions;
            Vector2Int size = display.UsesDesktopResolution
                ? DesktopResolution : new Vector2Int(display.Width, display.Height);
            FullScreenMode mode = ToFullScreenMode(display.WindowMode);
            if (mode == FullScreenMode.ExclusiveFullScreen)
            {
                size = ClosestSize(size, sizes);
                if (!display.UsesDesktopResolution) display.SetResolution(size.x, size.y);
                IReadOnlyList<RefreshRate> rates = RefreshRatesFor(size);
                RefreshRate rate = new RefreshRate { numerator = display.RefreshNumerator,
                    denominator = display.RefreshDenominator == 0 ? 1u : display.RefreshDenominator };
                if (rate.numerator != 0)
                {
                    bool found = false;
                    foreach (RefreshRate available in rates)
                        found |= SameRate(available, rate);
                    if (!found)
                    {
                        rate = rates.Count > 0 ? rates[0] : CurrentRefreshRate;
                        display.SetRefreshRate(rate.numerator, rate.denominator);
                    }
                }
                // Re-entering the same exclusive mode resets the output, which minimizes and restores the window.
                if (!IsCurrent(size, mode) || (rate.numerator != 0 && !SameRate(rate, CurrentRefreshRate)))
                    Screen.SetResolution(size.x, size.y, mode, rate);
            }
            else if (!IsCurrent(size, mode)) Screen.SetResolution(size.x, size.y, mode);

            await Awaitable.NextFrameAsync();
            if (_windowNeedsCentering && mode == FullScreenMode.Windowed)
                await MoveAsync(CurrentMonitor, display.WindowMode);
            _windowNeedsCentering = false;
        }

        private DisplayInfo ResolveMonitor(DisplaySettingsData display)
        {
            IReadOnlyList<DisplayInfo> monitors = Monitors;
            if (display.MonitorName.Length == 0) return CurrentMonitor;
            if (display.MonitorIndex < monitors.Count && monitors[display.MonitorIndex].name == display.MonitorName)
                return monitors[display.MonitorIndex];
            foreach (DisplayInfo monitor in monitors)
                if (monitor.name == display.MonitorName) return monitor;
            return CurrentMonitor;
        }

        private static async Task MoveAsync(DisplayInfo monitor, DisplayWindowMode mode)
        {
            Vector2Int position = Vector2Int.zero;
            if (mode == DisplayWindowMode.Windowed)
                position = new Vector2Int(monitor.workArea.x + Mathf.Max(0, (monitor.workArea.width - Screen.width) / 2),
                    monitor.workArea.y + Mathf.Max(0, (monitor.workArea.height - Screen.height) / 2));
            AsyncOperation move = Screen.MoveMainWindowTo(in monitor, position);
            while (!move.isDone) await Awaitable.NextFrameAsync();
        }

        private static bool IsCurrent(Vector2Int size, FullScreenMode mode) =>
            Screen.fullScreenMode == mode && Screen.width == size.x && Screen.height == size.y;

        private static bool SameRate(RefreshRate a, RefreshRate b) =>
            (ulong)a.numerator * b.denominator == (ulong)b.numerator * a.denominator;

        private static Vector2Int ClosestSize(Vector2Int requested, IReadOnlyList<Vector2Int> sizes)
        {
            if (sizes.Count == 0)
                throw new System.InvalidOperationException("Exclusive fullscreen requires a supported display resolution.");
            Vector2Int closest = sizes[0];
            double distance = double.MaxValue;
            foreach (Vector2Int size in sizes)
            {
                double dx = (double)size.x - requested.x;
                double dy = (double)size.y - requested.y;
                double candidate = dx * dx + dy * dy;
                if (candidate >= distance) continue;
                closest = size;
                distance = candidate;
            }
            return closest;
        }

        private static FullScreenMode ToFullScreenMode(DisplayWindowMode windowMode)
        {
            switch (windowMode)
            {
                case DisplayWindowMode.Windowed:
                    return FullScreenMode.Windowed;
                case DisplayWindowMode.ExclusiveFullscreen:
                    return FullScreenMode.ExclusiveFullScreen;
                case DisplayWindowMode.MaximizedWindow:
                    return FullScreenMode.MaximizedWindow;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }
    }
}
