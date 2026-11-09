using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TutorialUi
{
    public sealed class HintInputGlyph : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text separator;
        [SerializeField] private LayoutElement iconLayout;
        [SerializeField] private Sprite mouseNeutral;
        [SerializeField] private Sprite mouseLeft;
        [SerializeField] private Sprite mouseRight;
        [SerializeField] private Sprite mouseWheel;
        [SerializeField] private Sprite keycap;

        public void Render(HintInputPrompt prompt)
        {
            separator.text = prompt.SeparatorBefore;
            separator.gameObject.SetActive(prompt.SeparatorBefore.Length > 0);
            Sprite sprite;
            switch (prompt.ControlPath.ToLowerInvariant())
            {
                case "<mouse>/leftbutton": sprite = mouseLeft; break;
                case "<mouse>/rightbutton": sprite = mouseRight; break;
                case "<mouse>/middlebutton":
                case "<mouse>/scroll":
                case "<mouse>/scroll/y": sprite = mouseWheel; break;
                case "<mouse>/position":
                case "<mouse>/delta": sprite = mouseNeutral; break;
                default: sprite = keycap; break;
            }
            bool isKey = sprite == keycap;
            icon.sprite = sprite;
            icon.type = isKey ? Image.Type.Sliced : Image.Type.Simple;
            icon.pixelsPerUnitMultiplier = isKey ? 4f : 1f;
            iconLayout.preferredWidth = isKey ? Mathf.Clamp(24f + prompt.Label.Length * 7f, 46f, 112f) : 42f;
            iconLayout.preferredHeight = isKey ? 48f : 64f;
            icon.preserveAspect = !isKey;
            label.text = isKey ? prompt.Label : string.Empty;
            label.gameObject.SetActive(isKey);
        }
    }
}
