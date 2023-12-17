using System;
using EmpireAtWar.Services.Input;
using UnityEngine;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.NavigationService
{
    public enum SelectionType
    {
        None = 0,
        Terrain = 1,
        Base = 2,
        Ship = 3,
    }

    public interface INavigationService : IService
    {
        event Action<SelectionType> OnTypeChanged;
        SelectionType SelectionType { get; }
        void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType);
    }

    public class NavigationService : Service, INavigationService, IInitializable, ILateDisposable
    {
        public event Action<SelectionType> OnTypeChanged;

        private ISelectable selectable;
        private readonly IInputService inputService;
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
            if (selectable == null) return;

            if (selectable.CanMove)
            {
                selectable.MoveToPosition(screenPosition);
            }
        }

        public void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType)
        {
            if (SelectionType != selectionType )
            {
                SelectionType = selectionType;
                OnTypeChanged?.Invoke(SelectionType);
            }
            
            if (selectable == null)
            {
                selectable = selectableObject;
                selectableObject.SetActive(true);
                return;
            }

            if (selectable == selectableObject)
            {
                selectable.SetActive(false);
                selectable = null;
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