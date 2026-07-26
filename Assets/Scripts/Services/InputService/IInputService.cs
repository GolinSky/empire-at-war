using System;
using UnityEngine;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.InputService
{
    public interface IInputService:IService
    {
        event Action<Vector2> OnSwipe; 
        event Action<Vector2> OnCameraMove;
        event Action OnLeftMousePressed;
        event Action<Vector2> OnPrimaryDragStarted;
        event Action<Vector2> OnPrimaryDragChanged;
        event Action<Vector2> OnPrimaryDragEnded;
        event Action<bool> OnBlocked;
        event Action<InputType,TouchPhase, Vector2> OnInput;
        
        TouchPhase CurrentTouchPhase { get;}
        
        Vector2 TouchPosition { get; }
        Vector2 CameraMove { get; }
        int TapCount { get; }

        event Action<Vector2>  OnEndDrag;
        event Action<float> OnZoom; 
    }
}
