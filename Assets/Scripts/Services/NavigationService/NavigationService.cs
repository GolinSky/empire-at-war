using System;
using EmpireAtWar.Services.Input;
using UnityEngine;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.NavigationService
{
    public interface INavigationService : IService
    {
        event Action<SelectionType> OnTypeChanged;
        SelectionType SelectionType { get; }
        
        ISelectable Selectable { get; }
        void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType);
        void RemoveSelectable();
    }

    public class NavigationService : Service, INavigationService, IInitializable, ILateDisposable
    {
        public event Action<SelectionType> OnTypeChanged;

        private readonly IInputService inputService;
        
        public ISelectable Selectable { get; private set; }
        public SelectionType SelectionType { get; private set; }

        public NavigationService(IInputService inputService)
        {
            this.inputService = inputService;
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
        }

        private void HandleInput(Vector2 screenPosition)
        {
            if (Selectable == null) return;

            if (Selectable.CanMove)
            {
                Selectable.MoveToPosition(screenPosition);
            }
        }

        public void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType)
        {
            if(Selectable != null) return;
            
            if(selectionType == SelectionType.Terrain) return;
            
            Selectable = selectableObject;
            selectableObject.SetActive(true);
            
            if (SelectionType != selectionType)
            {
                SelectionType = selectionType;
                OnTypeChanged?.Invoke(SelectionType);
            }
        }

        public void RemoveSelectable()
        {
            if (Selectable != null)
            {
                Selectable.SetActive(false);
                Selectable = null;
                SelectionType = SelectionType.None;
                OnTypeChanged?.Invoke(SelectionType);
            }
        }

        protected override void OnInit(IGameObserver gameObserver)
        {
        }

        protected override void Release()
        {
        }
    }
}