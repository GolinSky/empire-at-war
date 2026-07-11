using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Models.Audio;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Audio
{
    public class AudioShipViewComponent: ViewComponent<IAudioShipModelObserver>
    {
        [SerializeField] private AudioSource source;


        private void Start()
        {
            PlayLoop(Model.AmbientClip);

        }

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
        
        private void PlayOneShot(AudioClip clip)
        {
            source.PlayOneShot(clip);
        }
        
        private void PlayLoop(AudioClip clip)
        {
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.Play(0);
        }
    }
}