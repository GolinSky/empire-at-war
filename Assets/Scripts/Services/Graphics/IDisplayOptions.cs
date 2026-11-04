using System.Collections.Generic;
using System.Threading.Tasks;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.Graphics
{
    public interface IDisplayOptions
    {
        /// <summary>Distinct supported output sizes of the current monitor, largest first.</summary>
        IReadOnlyList<Vector2Int> Resolutions { get; }

        Vector2Int DesktopResolution { get; }
        IReadOnlyList<DisplayWindowMode> WindowModes { get; }
        IReadOnlyList<RefreshRate> RefreshRatesFor(Vector2Int size);
        RefreshRate CurrentRefreshRate { get; }
        IReadOnlyList<DisplayInfo> Monitors { get; }
        DisplayInfo CurrentMonitor { get; }
        Task PendingApply { get; }
        Task ApplyDisplayAsync(DisplaySettingsData display);
        Task MoveToMonitorAsync(DisplaySettingsData display);
        DisplaySettingsData CaptureCurrentDisplay();
    }
}
