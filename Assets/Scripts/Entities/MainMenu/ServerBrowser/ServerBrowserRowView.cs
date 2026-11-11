using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.ServerBrowser
{
    /// <summary>One listed lobby; clicking the row joins it.</summary>
    public sealed class ServerBrowserRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text hostText;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text playersText;
        private UnityAction _clicked;

        public void Initialize(UnityAction clicked)
        {
            _clicked = clicked;
            button.onClick.AddListener(_clicked);
        }

        public void Render(ServerBrowserRowState state, bool interactable)
        {
            hostText.text = state.Host;
            modeText.text = state.Mode;
            playersText.text = state.Players;
            button.interactable = interactable;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void Dispose()
        {
            if (_clicked == null) return;
            button.onClick.RemoveListener(_clicked);
            _clicked = null;
        }
    }
}
