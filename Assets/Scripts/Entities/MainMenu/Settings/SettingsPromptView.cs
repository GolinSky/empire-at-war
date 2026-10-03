using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Full-screen blocker with a message and up to three answer buttons.</summary>
    public class SettingsPromptView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button[] actionButtons;
        [SerializeField] private TMP_Text[] actionLabels;
        private SettingsPrompt _prompt = SettingsPrompt.None;

        public event Action<SettingsPromptAction> ActionChosen;

        public void Initialize()
        {
            for (int i = 0; i < actionButtons.Length; i++)
            {
                int buttonIndex = i;
                actionButtons[i].onClick.AddListener(() => ActionChosen?.Invoke(_prompt.Actions[buttonIndex]));
            }
        }

        public void Dispose()
        {
            foreach (Button button in actionButtons)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        public void Render(SettingsPrompt prompt)
        {
            _prompt = prompt;
            gameObject.SetActive(prompt.IsVisible);
            messageText.text = prompt.Message;
            for (int i = 0; i < actionButtons.Length; i++)
            {
                bool hasAction = i < prompt.Actions.Count;
                actionButtons[i].gameObject.SetActive(hasAction);
                if (hasAction)
                {
                    actionLabels[i].text = prompt.Actions[i].ToString();
                }
            }
        }
    }
}
