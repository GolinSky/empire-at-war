using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    public sealed class UnitHighlightService : IInitializable
    {
        private static readonly int HIGHLIGHT_COLOR_ID = Shader.PropertyToID("_UnitHighlightColor");
        private static readonly int HIGHLIGHT_STRENGTH_ID = Shader.PropertyToID("_UnitHighlightStrength");

        private readonly UnitHighlightSettings _settings;

        public UnitHighlightService(UnitHighlightSettings settings)
        {
            _settings = settings;
        }

        public void Initialize()
        {
            Shader.SetGlobalVector(HIGHLIGHT_COLOR_ID, _settings.Color.linear);
            Shader.SetGlobalVector(HIGHLIGHT_STRENGTH_ID,
                new Vector4(_settings.HoverStrength, _settings.SelectedStrength, 0f, 0f));
        }
    }
}
