using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    [Serializable]
    public sealed class DisplaySettingsData
    {
        [SerializeField] private string windowMode = nameof(DisplayWindowMode.Borderless);

        [SerializeField] private int width;
        [SerializeField] private int height;

        public DisplayWindowMode WindowMode
        {
            get => (DisplayWindowMode)Enum.Parse(typeof(DisplayWindowMode), windowMode);
            set => windowMode = value.ToString();
        }

        /// <summary>Zero width and height mean the desktop resolution.</summary>
        public int Width => width;
        public int Height => height;
        public bool UsesDesktopResolution => width <= 0 || height <= 0;

        public void SetResolution(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
        }

        public bool Matches(DisplaySettingsData other)
        {
            return windowMode == other.windowMode && width == other.width && height == other.height;
        }

        public DisplaySettingsData Clone()
        {
            return (DisplaySettingsData)MemberwiseClone();
        }

        public void Sanitize()
        {
            if (!Enum.TryParse(windowMode, out DisplayWindowMode _))
            {
                windowMode = nameof(DisplayWindowMode.Borderless);
            }

            if (width <= 0 || height <= 0)
            {
                width = 0;
                height = 0;
            }
        }
    }
}
