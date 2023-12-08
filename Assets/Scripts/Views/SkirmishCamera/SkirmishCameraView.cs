using System;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.SkirmishCamera
{
    public class SkirmishCameraView:View<ISkirmishCameraModelObserver>
    {
        public float panSpeed = 20f; // How fast the camera pans
        public float panBorderThickness = 10f; // How close to the screen edge the mouse needs to be to start panning
        public float scrollSpeed = 20f; // How fast the camera zooms
        public float minY = 10f; // Minimum camera height
        public float maxY = 80f; // Maximum camera height

        private bool doMovement = true;

        void Update()
        {

            if (!doMovement)
                return;

            // Camera panning
            if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - panBorderThickness)
                transform.Translate(Vector3.forward * panSpeed * Time.deltaTime, Space.World);

            if (Input.GetKey("s") || Input.mousePosition.y <= panBorderThickness)
                transform.Translate(Vector3.back * panSpeed * Time.deltaTime, Space.World);

            if (Input.GetKey("a") || Input.mousePosition.x <= panBorderThickness)
                transform.Translate(Vector3.left * panSpeed * Time.deltaTime, Space.World);

            if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - panBorderThickness)
                transform.Translate(Vector3.right * panSpeed * Time.deltaTime, Space.World);

            // Camera zooming
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Vector3 pos = transform.position;

            pos.y -= scroll * scrollSpeed * 1000 * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }
        protected override void OnInitialize()
        {
            
        }

        protected override void OnDispose()
        {
            
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            doMovement = hasFocus;
        }
    }
}