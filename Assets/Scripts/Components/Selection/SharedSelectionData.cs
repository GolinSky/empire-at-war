using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Selection
{
    [CreateAssetMenu(fileName = "SharedSelectionData", menuName = "Data/Selection/Shared Selection Data")]
    public class SharedSelectionData : Data
    {
        [SerializeField] private Sprite selectionSprite;

        public Sprite SelectionSprite => selectionSprite;
    }
}
