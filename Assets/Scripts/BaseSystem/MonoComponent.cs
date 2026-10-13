using UnityEngine;

namespace EmpireAtWar.Mvc
{
    public interface IMonoComponent
    {
        void Release();
    }

    public abstract class MonoComponent<TModel>: MonoBehaviour, IMonoComponent
        where TModel : Model
    {
        public string Id { get; }// remove this
        protected TModel Model { get; private set; }

        protected void SetModel(TModel model)
        {
            Model = model;
        }

        public virtual void Release()
        {
        }
    }
}
