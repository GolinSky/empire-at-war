using System;
using EmpireAtWar.Models.Factions;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.Models.ShipUi
{
    public interface IShipUiModelObserver:IModelObserver
    {
        event Action<SelectionType> OnSelectionChanged;
        event Action<Vector2> OnTapPositionChanged;
        event Action OnSkipGoToPositionUi;
        Sprite ShipIcon { get; }
    }
    
    [CreateAssetMenu(fileName = "ShipUiModel", menuName = "Model/ShipUiModel")]
    public class ShipUiModel : Model, IShipUiModelObserver
    {
        public event Action<Vector2> OnTapPositionChanged;
        public event Action OnSkipGoToPositionUi;
        public event Action<SelectionType> OnSelectionChanged;
        
        private Vector2 _tapPosition;

        [FormerlySerializedAs("shipUiWrapper")] [SerializeField] private DictionaryWrapper<ShipType, Sprite> shipIconWrapper;
        public Sprite ShipIcon { get; set; }

        public Vector2 TapPosition
        {
            set
            {
                _tapPosition = value;
                OnTapPositionChanged?.Invoke(_tapPosition);
            }
            get => _tapPosition;
        }

        public void UpdateSelection(SelectionType selectionType) => OnSelectionChanged?.Invoke(selectionType);

        public Sprite GetShipIcon(ShipType shipType)
        {
            return shipIconWrapper.Dictionary[shipType];
        }

        public void SkipGoToPositionUi()
        {
            OnSkipGoToPositionUi?.Invoke();
        }
    }
}