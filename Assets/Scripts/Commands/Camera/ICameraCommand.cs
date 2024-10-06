using LightWeightFramework.Command;
using UnityEngine;

namespace EmpireAtWar.Commands.Camera
{
    public interface ICameraCommand:ICommand
    {
        void MoveTo(Vector3 worldPoint);
    }
}