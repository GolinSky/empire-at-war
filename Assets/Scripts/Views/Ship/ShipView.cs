using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.Ship
{
    public class ShipView : View<IShipModelObserver, IShipCommand>
    {
        RaycastHit m_Hit;
        [SerializeField]private float m_MaxDistance = 0;
        bool m_HitDetect;
        [SerializeField] private LayerMask layerMask;
        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
        }

        public override void Release()
        {
            base.Release();
            Destroy(gameObject,2f);
        }
        // void FixedUpdate()
        // {
        //
        //
        //
        //     m_HitDetect = Physics.BoxCast(Center, Vector3.one*40, Vector3.up, out m_Hit, Quaternion.identity, m_MaxDistance, layerMask.value);
        //     if (m_HitDetect )
        //     {
        //         //Output the name of the Collider your Box hit
        //         Debug.Log("Hit : " + m_Hit.collider.name);
        //     }
        // }

        private Vector3 Center => transform.position - Vector3.up * 50;
        
        // void OnDrawGizmos()
        // {
        //
        //     if (m_HitDetect)
        //     {
        //         Gizmos.color = Color.red;
        //         Gizmos.DrawRay(Center, Vector3.up * m_Hit.distance);
        //         Gizmos.DrawWireCube(Center + Vector3.up * m_Hit.distance, Vector3.one*40*2);
        //     }
        //     else
        //     {
        //         Gizmos.color = Color.green;
        //
        //         Gizmos.DrawRay(Center, Vector3.up * m_MaxDistance);
        //         Gizmos.DrawWireCube(Center + Vector3.up * m_MaxDistance, Vector3.one*40*2);
        //     }
        // }
    }
}