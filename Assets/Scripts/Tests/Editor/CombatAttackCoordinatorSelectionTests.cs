using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class CombatAttackCoordinatorSelectionTests
    {
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void CaptureTargetSelections_ReadsTransformsAfterQueueAtEveryThreshold(int requestCount)
        {
            CombatAttackCoordinator coordinator = new CombatAttackCoordinator();
            List<GameObject> owners = new List<GameObject>();
            try
            {
                for (int i = 0; i < requestCount; i++)
                {
                    GameObject ownerObject = new GameObject("WeaponOwner");
                    owners.Add(ownerObject);
                    WeaponComponent weapon = ownerObject.AddComponent<WeaponComponent>();
                    GameObject hardPointObject = new GameObject("HardPoint");
                    hardPointObject.transform.SetParent(ownerObject.transform);
                    WeaponHardPoint hardPoint = hardPointObject.AddComponent<WeaponHardPoint>();
                    typeof(WeaponComponent).GetField("hardPoints", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(weapon, new List<WeaponHardPoint> { hardPoint });
                    typeof(WeaponComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(weapon, new object[] { coordinator, new CombatModifiers() });
                    coordinator.Register(weapon);
                    coordinator.QueueTargetSelection(weapon, hardPoint);
                }

                owners[0].transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f),
                    Quaternion.Euler(0f, 40f, 0f));
                typeof(CombatAttackCoordinator).GetMethod("CaptureTargetSelections",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(coordinator, null);

                IList requests = (IList)typeof(CombatAttackCoordinator)
                    .GetField("_targetSelectionRequests", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(coordinator);
                object firstRequest = requests[0];
                Vector3 origin = (Vector3)firstRequest.GetType().GetField("Origin").GetValue(firstRequest);
                Quaternion parentRotation = (Quaternion)firstRequest.GetType()
                    .GetField("ParentRotation").GetValue(firstRequest);
                Assert.That(requests.Count, Is.EqualTo(requestCount));
                Assert.That(origin, Is.EqualTo(new Vector3(3f, 4f, 5f)));
                Assert.That(Quaternion.Angle(parentRotation, Quaternion.Euler(0f, 40f, 0f)),
                    Is.LessThan(0.01f));
            }
            finally
            {
                foreach (GameObject owner in owners) Object.DestroyImmediate(owner);
                coordinator.Dispose();
            }
        }

        [Test]
        public void LateTick_IgnoresQueuedRequestAfterOwnerRelease()
        {
            CombatAttackCoordinator coordinator = new CombatAttackCoordinator();
            GameObject ownerObject = new GameObject("WeaponOwner");
            try
            {
                WeaponComponent weapon = ownerObject.AddComponent<WeaponComponent>();
                GameObject hardPointObject = new GameObject("HardPoint");
                hardPointObject.transform.SetParent(ownerObject.transform);
                WeaponHardPoint hardPoint = hardPointObject.AddComponent<WeaponHardPoint>();
                typeof(WeaponComponent).GetField("hardPoints", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(weapon, new List<WeaponHardPoint> { hardPoint });
                typeof(WeaponComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(weapon, new object[] { coordinator, new CombatModifiers() });
                coordinator.Register(weapon);
                coordinator.QueueTargetSelection(weapon, hardPoint);
                weapon.Release();

                coordinator.LateTick();

                IList requests = (IList)typeof(CombatAttackCoordinator)
                    .GetField("_targetSelectionRequests", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(coordinator);
                Assert.That(requests.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
                coordinator.Dispose();
            }
        }
    }
}
