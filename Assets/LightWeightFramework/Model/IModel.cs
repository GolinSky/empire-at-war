namespace LightWeightFramework.Model
{
    public interface IModelObserver
    {
        TModelObserver GetModelObserver<TModelObserver>() where TModelObserver: IModelObserver;
    }
    
    public interface IModel:IModelObserver
    {
        void Init();
        TModelObserver GetModel<TModelObserver>() where TModelObserver: IModel;

    }
}