using System;
using UnityEngine;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.InputService
{
    public interface IInputService:IService
    {
        event Action<Vector2> OnSwipe; 
        event Action OnLeftMousePressed;
        event Action<bool> OnBlocked;
        event Action<InputType,TouchPhase, Vector2> OnInput;
        
        TouchPhase CurrentTouchPhase { get;}
        
        Vector2 TouchPosition { get; }

        event Action<Vector2>  OnEndDrag;
        event Action<float> OnZoom; 
    }
}
