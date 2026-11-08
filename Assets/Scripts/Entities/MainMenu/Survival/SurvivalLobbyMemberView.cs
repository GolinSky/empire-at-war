using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    /// <summary>One commander chip of the online Survival lobby: Steam avatar (or initial), name and ready state.</summary>
    public sealed class SurvivalLobbyMemberView : MonoBehaviour
    {
        [SerializeField] private RawImage avatarImage;
        [SerializeField] private TMP_Text initialText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Color readyColor = new Color(0.22f, 0.74f, 0.97f);
        [SerializeField] private Color waitingColor = new Color(0.58f, 0.64f, 0.72f);

        public void Render(SurvivalLobbyMemberState state)
        {
            bool hasAvatar = state.Avatar != null;
            avatarImage.gameObject.SetActive(hasAvatar);
            avatarImage.texture = state.Avatar;
            initialText.gameObject.SetActive(!hasAvatar);
            initialText.text = state.Name.Length > 0 ? state.Name.Substring(0, 1).ToUpperInvariant() : "?";
            nameText.text = state.Name;
            statusText.text = state.Status;
            statusText.color = state.IsReady ? readyColor : waitingColor;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
