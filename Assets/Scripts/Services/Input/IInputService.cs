using System;
using UnityEngine;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Input
{
    public interface IInputService:IService
    {
        event Action<InputType,TouchPhase, Vector2> OnInput;
        TouchPhase LastTouchPhase { get;}
    }
}