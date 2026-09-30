using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Render state of the settings screen. Values mirror the settings draft; the presenter writes them.</summary>
    public class SettingsModel : PureModel, ISettingsModelObserver
    {
        public event Action Changed;

        public SettingsChoice Quality { get; private set; } = SettingsChoice.Empty;
        public SettingsChoice WindowMode { get; private set; } = SettingsChoice.Empty;
        public SettingsChoice Resolution { get; private set; } = SettingsChoice.Empty;
        public SettingsChoice FrameRateLimit { get; private set; } = SettingsChoice.Empty;
        public bool VSync { get; private set; }

        public float PanSpeed { get; private set; }
        public float ZoomSpeed { get; private set; }
        public bool EdgeScrolling { get; private set; }
        public bool InvertZoom { get; private set; }

        public IReadOnlyList<KeyBindingRowState> Bindings { get; private set; } = new KeyBindingRowState[0];

        public bool IsDirty { get; private set; }
        public string StatusMessage { get; private set; } = string.Empty;
        public SettingsPrompt Prompt { get; private set; } = SettingsPrompt.None;

        public void SetGraphics(
            SettingsChoice quality,
            SettingsChoice windowMode,
            SettingsChoice resolution,
            SettingsChoice frameRateLimit,
            bool vSync)
        {
            Quality = quality;
            WindowMode = windowMode;
            Resolution = resolution;
            FrameRateLimit = frameRateLimit;
            VSync = vSync;
            Changed?.Invoke();
        }

        public void SetCamera(float panSpeed, float zoomSpeed, bool edgeScrolling, bool invertZoom)
        {
            PanSpeed = panSpeed;
            ZoomSpeed = zoomSpeed;
            EdgeScrolling = edgeScrolling;
            InvertZoom = invertZoom;
            Changed?.Invoke();
        }

        public void SetBindings(IReadOnlyList<KeyBindingRowState> bindings)
        {
            Bindings = bindings;
            Changed?.Invoke();
        }

        public void SetDirty(bool isDirty)
        {
            IsDirty = isDirty;
            Changed?.Invoke();
        }

        public void SetStatus(string statusMessage)
        {
            StatusMessage = statusMessage;
            Changed?.Invoke();
        }

        public void SetPrompt(SettingsPrompt prompt)
        {
            Prompt = prompt;
            Changed?.Invoke();
        }
    }
}
