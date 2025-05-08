using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionSubject>
    {
       // void UpdateSelectable(ISelectable selectable, SelectionType selectionType);
        void RemoveSelectable(ISelectionContext selectionContext);
    }

    public class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable, ISelectionSubject
    {

        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly IEntityMediator _entityMediator;
        private readonly List<IObserver<ISelectionSubject>> _observers = new List<IObserver<ISelectionSubject>>();
      
        private readonly SelectionContext _playerSelectionContext = new SelectionContext();
        private readonly SelectionContext _enemySelectionContext = new SelectionContext();


        public ISelectionContext PlayerSelectionContext => _playerSelectionContext;
        public ISelectionContext EnemySelectionContext => _enemySelectionContext;
        public PlayerType UpdatedType { get; private set; }

        public SelectionService(IInputService inputService, ICameraService cameraService, IEntityMediator entityMediator)
        {
            _inputService = inputService;
            _cameraService = cameraService;
            _entityMediator = entityMediator;
        }
        
        public void Initialize()
        {
            _inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
        }
  

        public void RemoveSelectable(ISelectionContext context)
        {
            RemoveSelectable(context.PlayerType);
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if(inputType != InputType.Selection) return;
            
            RaycastHit raycastHit = _cameraService.ScreenPointToRay(touchPosition);

            if(raycastHit.collider == null) return;


            if (_entityMediator.TryGetEntity(raycastHit, out IEntity entity))
            {
                if (entity.TryGetCommand(out IEntitySelectionCommand command))
                {
                    RemoveSelectable(entity.PlayerType);

                    switch (entity.PlayerType)
                    {
                        case PlayerType.Player:
                        {
                            UpdateContext(_playerSelectionContext);
                            break;
                        }
                        case PlayerType.Opponent:
                        {
                            UpdateContext(_enemySelectionContext);
                            break;
                        }
                    }
                }
                NotifyObservers(entity.PlayerType);

                
                void UpdateContext(SelectionContext selectionContext)
                {
                    selectionContext.Update(entity, command, command.SelectionType, entity.PlayerType);
                    selectionContext.SetSelectableState(true);
                }
                //viewEntity.Id
            }
        }
        
        private void RemoveSelectable(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    _playerSelectionContext.ResetCurrentSelectable();
                    break;
                case PlayerType.Opponent:
                    _enemySelectionContext.ResetCurrentSelectable();
                    break;
            }
            NotifyObservers(playerType);
        }
        
        private void NotifyObservers(PlayerType playerType)
        {
            UpdatedType = playerType;
            for (var i = 0; i < _observers.Count; i++)
            {
                _observers[i].UpdateState(this);
            }
        }

        public void AddObserver(IObserver<ISelectionSubject> observer)
        {
            _observers.Add(observer);
        }

        public void RemoveObserver(IObserver<ISelectionSubject> observer)
        {
            _observers.Remove(observer);
        }
        
    }
}