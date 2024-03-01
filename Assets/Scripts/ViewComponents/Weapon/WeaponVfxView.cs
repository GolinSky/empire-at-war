using UnityEngine;

namespace EmpireAtWar
{
    public class WeaponVfxView : MonoBehaviour, IObserver<float>
    {
        [SerializeField] private ParticleSystem explosionVfx;

        private INotifier<float> notifier;
        private void Start()
        {
            notifier = GetComponent<INotifier<float>>();
            notifier.AddObserver(this);
            
            explosionVfx.transform.SetParent(transform.parent);// todo: refactor
        }

        private void OnDestroy()
        {
            notifier.RemoveObserver(this);
        }

        public void UpdateState(float value)
        {
            if (value <= 0)
            {
                explosionVfx.Play();
            }
        }
    }
}
