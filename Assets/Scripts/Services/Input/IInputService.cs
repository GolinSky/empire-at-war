using System;
using UnityEngine;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Input
{
    public interface IInputService:IService
    {
        Vector2 MouseCoordinates { get; }
        event Action<Vector2> OnInput;
    }
}