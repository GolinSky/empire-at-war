using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    public interface IFactionModelObserver : IModelObserver
    {
        FactionUnitUi ShipUnit { get; }
    }

    [CreateAssetMenu(fileName = "FactionModel", menuName = "Model/FactionModel")]
    public class FactionModel : FactionModel<RepublicShipType>, IFactionModelObserver
    {
        [field: SerializeField] public FactionUnitUi ShipUnit { get; private set; }
    }
}