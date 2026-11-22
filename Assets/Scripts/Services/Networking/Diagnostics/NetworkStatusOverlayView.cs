using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>IMGUI network status overlay (top right); shown while the cheat panel's Debug toggle is on.</summary>
    public sealed class NetworkStatusOverlayView : MonoBehaviour
    {
        private const float WIDTH = 820f;
        private const float MARGIN = 16f;
        private const float MAX_HEIGHT_FRACTION = 0.8f;
        private const int FONT_SIZE = 12;
        private static readonly Color BACKGROUND = new Color32(10, 16, 26, 220);

        private NetworkStatusModel _model;
        private GUIStyle _labelStyle;
        private Texture2D _background;
        private Vector2 _scroll;

        [Inject]
        private void Construct(NetworkStatusModel model) => _model = model;

        private void OnGUI()
        {
            if (!_model.IsVisible) return;
            if (_labelStyle == null)
            {
                _background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _background.SetPixel(0, 0, BACKGROUND);
                _background.Apply(false, true);
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = FONT_SIZE, wordWrap = true, richText = false };
            }

            float width = Mathf.Min(WIDTH, Screen.width - 2f * MARGIN);
            Rect area = new Rect(Screen.width - width - MARGIN, MARGIN, width, Screen.height * MAX_HEIGHT_FRACTION);
            GUI.DrawTexture(area, _background);
            GUILayout.BeginArea(area);
            _scroll = GUILayout.BeginScrollView(_scroll);
            IReadOnlyList<string> lines = _model.Lines;
            for (int i = 0; i < lines.Count; i++) GUILayout.Label(lines[i], _labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (_background != null) Destroy(_background);
        }
    }
}
