using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public sealed class FactionTypeFilterButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public Button Button => button;
        public TMP_Text Label => label;
    }
}
