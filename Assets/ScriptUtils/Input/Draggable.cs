using System;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace EmpireAtWar
{
    [RequireComponent(typeof(RectTransform))]
    public class Draggable : MonoBehaviour
    {
        [Inject] private ICameraService cameraService;
        [Inject] private InputService inputService;
        private RectTransform rectTransform;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // public void OnDrag(PointerEventData eventData)
        // {
        //     rectTransform.anchoredPosition += eventData.delta;
        // }

        private void OnMouseDrag()
        {
            inputService.Block(true);
            Vector2 delta = Input.GetTouch(0).position;
            Vector3 position = cameraService.GetWorldPoint(delta);
            position.y = 0;
            transform.position = position;
        }

        private void OnMouseExit()
        {
            inputService.Block(false);

        }
    }
}
