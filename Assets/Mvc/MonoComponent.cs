using UnityEngine;
using Zenject;

namespace EmpireAtWar.Mvc
{
    public abstract class MonoComponent<TModel>: MonoBehaviour
        where TModel : PureModel
    {
        public string Id { get; }// remove this
        protected TModel Model { get; private set; }

        [Inject]
        private void InjectDependencies(TModel model)
        {
            Model = model;
        }
    }
}