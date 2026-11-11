using System;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>
    /// Host-only Skirmish / Survival switch on the lobby setup screens. Hidden offline and for clients; the
    /// screen title names the current mode.
    /// </summary>
    public sealed class LobbyModeSwitchView : MonoBehaviour
    {
        [SerializeField] private Button skirmishButton;
        [SerializeField] private Button survivalButton;
        private bool _isInitialized;

        public event Action<LobbyMode> ModeSelected;

        public void Initialize()
        {
            if (_isInitialized) return;
            skirmishButton.onClick.AddListener(SelectSkirmish);
            survivalButton.onClick.AddListener(SelectSurvival);
            Hide();
            _isInitialized = true;
        }

        /// <param name="survivalAllowed">False while more members are in than Survival seats.</param>
        public void Render(LobbyMode current, bool survivalAllowed)
        {
            skirmishButton.interactable = current != LobbyMode.Skirmish;
            survivalButton.interactable = current != LobbyMode.Survival && survivalAllowed;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void Dispose()
        {
            if (!_isInitialized) return;
            skirmishButton.onClick.RemoveListener(SelectSkirmish);
            survivalButton.onClick.RemoveListener(SelectSurvival);
            _isInitialized = false;
        }

        private void SelectSkirmish() => ModeSelected?.Invoke(LobbyMode.Skirmish);
        private void SelectSurvival() => ModeSelected?.Invoke(LobbyMode.Survival);
    }
}
