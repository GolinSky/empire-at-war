using System;
using EmpireAtWar.Models.Audio;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents.Audio
{
    public class AudioShipViewComponent: ViewComponent<IAudioShipModelObserver>, IInitializable, ILateDisposable
    {
        [SerializeField] private AudioSource audioSource;

        protected override void OnInit()
        {
            base.OnInit();
            Model.OnOneShotPlayed += PlayOneShot;
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            Model.OnOneShotPlayed -= PlayOneShot;

        }

        public void Initialize()
        {
        }

        public void LateDispose()
        {
            Model.OnOneShotPlayed -= PlayOneShot;
        }
        
        private void PlayOneShot(AudioClip audioClip)
        {
             audioSource.PlayOneShot(audioClip);
        }
    }
}