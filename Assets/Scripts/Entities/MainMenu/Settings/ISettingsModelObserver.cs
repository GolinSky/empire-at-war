using System;
using System.Collections.Generic;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public interface ISettingsModelObserver
    {
        event Action Changed;

        SettingsChoice Quality { get; }
        SettingsChoice WindowMode { get; }
        SettingsChoice Resolution { get; }
        SettingsChoice FrameRateLimit { get; }
        bool VSync { get; }

        float MasterVolume { get; }
        float MusicVolume { get; }
        float VoiceVolume { get; }
        float SfxVolume { get; }
        bool MuteWhenUnfocused { get; }

        float PanSpeed { get; }
        float ZoomSpeed { get; }
        bool EdgeScrolling { get; }
        bool InvertZoom { get; }

        IReadOnlyList<KeyBindingRowState> Bindings { get; }

        bool IsDirty { get; }
        string StatusMessage { get; }
        SettingsPrompt Prompt { get; }
    }
}
