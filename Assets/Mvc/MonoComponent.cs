using UnityEngine;
using Zenject;

namespace EmpireAtWar.Mvc
{
    public abstract class MonoComponent<TModel>: MonoBehaviour
        where TModel : PureModel
    {
        protected TModel Model { get; private set; }

        [Inject]
        private void InjectDependencies(TModel model)
        {
            Model = model;
        }
    }
}