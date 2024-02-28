using System;
using UnityEngine;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.InputService
{
    public interface IInputService:IService
    {
        event Action<InputType,TouchPhase, Vector2> OnInput;
        event Action<InputType, Touch, Touch> OnDoubleInput;
        TouchPhase CurrentTouchPhase { get;}
        
        Vector2 TouchPosition { get; }

        event Action<Vector2>  OnEndDrag;
    }
}