using UnityEngine;
using Zenject;

namespace EmpireAtWar.Mvc
{
    public interface IMonoComponent
    {
        void Release();
    }

    public abstract class MonoComponent<TModel>: MonoBehaviour, IMonoComponent
        where TModel : PureModel
    {
        public string Id { get; }// remove this
        protected TModel Model { get; private set; }

        [Inject]
        private void InjectDependencies([InjectOptional] TModel model)
        {
            Model = model;
        }

        protected void SetModel(TModel model)
        {
            Model = model;
        }

        public virtual void Release()
        {
        }
    }
}
