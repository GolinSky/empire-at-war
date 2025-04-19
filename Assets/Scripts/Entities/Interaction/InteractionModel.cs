using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Entities.Interaction
{
    public interface IInteractionModelObserver: IModelObserver
    {
        
    }
    [CreateAssetMenu(fileName = "EconomyModel", menuName = "Model/InteractionModel")]
    public class InteractionModel: Model, IInteractionModelObserver
    {
        
    }
}