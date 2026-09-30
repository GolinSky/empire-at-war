using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.Graphics
{
    /// <summary>
    /// Applies window mode and output size. Borderless always uses the desktop resolution.
    /// Unity applies the change at the end of the frame; the settings screen confirms it afterwards.
    /// </summary>
    public sealed class DisplaySettingsApplier : ISettingsApplier, IDisplayOptions
    {
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

        public void Apply(SettingsData settings)
        {
            DisplaySettingsData display = settings.Display;
            FullScreenMode mode = ToFullScreenMode(display.WindowMode);
            Vector2Int size = display.WindowMode == DisplayWindowMode.Borderless || display.UsesDesktopResolution
                ? DesktopResolution
                : new Vector2Int(display.Width, display.Height);

            if (Screen.fullScreenMode == mode && Screen.width == size.x && Screen.height == size.y)
            {
                return;
            }

            Screen.SetResolution(size.x, size.y, mode);
        }

        private static FullScreenMode ToFullScreenMode(DisplayWindowMode windowMode)
        {
            switch (windowMode)
            {
                case DisplayWindowMode.Windowed:
                    return FullScreenMode.Windowed;
                case DisplayWindowMode.ExclusiveFullscreen:
                    return FullScreenMode.ExclusiveFullScreen;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }
    }
}
