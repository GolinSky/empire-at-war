using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public sealed class SimulationFleetRowUi : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button removeButton;

        public void Initialize(string text, UnityAction remove)
        {
            label.text = text;
            removeButton.onClick.AddListener(remove);
        }
    }
}
