using System;
using UnityEngine;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Input
{
    public interface IInputService:IService
    {
        Vector2 MouseCoordinates { get; }
        event Action<InputType,TouchPhase, Vector2> OnInput;
        
    }
}