using System;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.NavigationService
{
    public interface INavigationService : IService
    {
        event Action<SelectionType> OnTypeChanged;
        SelectionType SelectionType { get; }

        ISelectable Selectable { get; }
        void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType);

        void RemoveSelectable(ISelectable selectable);
        void RemoveSelectable();
    }

    public class NavigationService : Service, INavigationService
    {
        public event Action<SelectionType> OnTypeChanged;

        private readonly IInputService _inputService;
        private IMovable _movable;

        public ISelectable Selectable { get; private set; }
        public SelectionType SelectionType { get; private set; }

        public NavigationService(IInputService inputService)
        {
            _inputService = inputService;
        }
        
        public void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType)
        {
            if (selectionType == SelectionType.Terrain) return;

            if (Selectable != null)
            {
                RemoveSelectable(Selectable);
            }

            Selectable = selectableObject;
            _movable = selectableObject.Movable;
            selectableObject.SetActive(true);

            SelectionType = selectionType;
            OnTypeChanged?.Invoke(SelectionType);
        }

        public void RemoveSelectable(ISelectable selectable)
        {
            if (Selectable == selectable)
            {
                Selectable?.SetActive(false);
                Selectable = null;
                OnTypeChanged?.Invoke(SelectionType.None);
            }
        }

        public void RemoveSelectable()
        {
            if (Selectable != null)
            {
                Selectable.SetActive(false);
                Selectable = null;
                _movable = null;
                SelectionType = SelectionType.None;
                OnTypeChanged?.Invoke(SelectionType);
            }
        }
    }
}