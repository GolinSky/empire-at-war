using UnityEngine;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Move
{
    public interface IMoveCommand:ICommand
    {
        void Assign(Transform transform);
    }
}