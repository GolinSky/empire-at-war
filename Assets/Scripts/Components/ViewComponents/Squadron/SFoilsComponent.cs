using UnityEngine;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    public sealed class SFoilsComponent : MonoBehaviour, ISFoilsComponent
    {
        [SerializeField] private Animation[] animations;
        [SerializeField] private AnimationClip opened;
        [SerializeField] private AnimationClip closed;

        public void SetClosed(bool isClosed, bool immediate)
        {
            AnimationClip clip = isClosed ? closed : opened;
            foreach (Animation animation in animations)
            {
                if (!animation.gameObject.activeInHierarchy) continue;
                animation.Play(clip.name);
                if (!immediate) continue;
                animation[clip.name].time = clip.length;
                animation.Sample();
                animation.Stop();
            }
        }
    }
}
