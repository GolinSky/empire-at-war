using LightWeightFramework.Command;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public abstract class BaseUi:MonoBehaviour
    {
        
    }
    public abstract class BaseUi<TModel> :  BaseUi
        where TModel : IModelObserver
    {
        [Inject]
        public TModel Model { get; }
        
    }

    public abstract class BaseUi<TModel, TCommand> : BaseUi<TModel>
        where TModel : IModelObserver
        where TCommand : ICommand
    {
        [Inject] protected TCommand Command { get; }
    }
}