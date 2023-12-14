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