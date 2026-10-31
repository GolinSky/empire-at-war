using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    public static class ShipOrderTelemetry
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float SAMPLE_INTERVAL = 5f;
        private const float FLUSH_INTERVAL = 10f;
        private const string HEADER = "timeScaledSinceStartup,timeUnscaledSinceStartup,kind,player,entityId," +
            "order,state,targetId,destinationX,destinationZ,x,z,moving,contacts,preferredContactId," +
            "preferredDistance,preferredInRange,trackedContactId,radarRange,weaponRange,visionMultiplier," +
            "ionDisabled,cloaked,facingDot,hull,shields,reason";
        private static readonly Dictionary<UnitOrderModel, float> _nextSample = new Dictionary<UnitOrderModel, float>();
        private static readonly StringBuilder _lines = new StringBuilder();
        private static string _path;
        private static float _nextFlush;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Reset(SceneManager.GetActiveScene().name);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting -= Flush;
            Application.quitting += Flush;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

#if UNITY_EDITOR
        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) Flush();
        }
#endif

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Flush();
            Reset(scene.name);
        }

        private static void Reset(string sceneName)
        {
            _lines.Clear();
            _nextSample.Clear();
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            _path = Path.Combine(Application.persistentDataPath, "BattleTelemetry",
                $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}_ai_orders.csv");
        }
#endif

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Sample(ShipOrderTelemetrySource source, IShipAbilityFacade caster, UnitOrderModel orders, IShipMovement movement,
            IWeaponComponent weapon, IRadarComponent radar, string state)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            if (_nextSample.TryGetValue(orders, out float next) && Time.time < next) return;
            Record(source, caster, orders, movement, weapon, radar, "sample", "Periodic order/contact state.", state);
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Record(ShipOrderTelemetrySource source, IShipAbilityFacade caster, UnitOrderModel orders, IShipMovement movement,
            IWeaponComponent weapon, IRadarComponent radar, string kind, string reason, string state)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            _nextSample[orders] = Time.time + SAMPLE_INTERVAL;
            IEntity preferred = ShipEngagement.FindNearestEnemy(radar, movement, weapon, inRangeOnly: false);
            IEntity tracked = null;
            for (int i = 0; i < radar.Enemies.Count; i++)
            {
                IEntity contact = radar.Enemies[i];
                if (!contact.HealthModel.IsDestroyed && contact.HealthModel.HasUnits &&
                    weapon.HasTarget(contact.GetFacade<IHealthFacade>()))
                {
                    tracked = contact;
                    break;
                }
            }
            Vector3 position = movement.CurrentPosition;
            Vector3 destination = ToVector(orders.Destination);
            IEntity facingTarget = tracked ?? preferred;
            float facingDot = 0f;
            if (facingTarget != null)
            {
                Vector3 direction = facingTarget.GetFacade<IEntityTransformFacade>().Transform.position - position;
                direction.y = 0f;
                facingDot = Vector3.Dot(source.Forward, direction.normalized);
            }
            float distance = preferred == null ? -1f :
                movement.GetRange(preferred.GetFacade<IEntityTransformFacade>().Transform.position);
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F2},{1:F2},{2},{3},{4},{5},{6},{7},{8:F1},{9:F1},{10:F1},{11:F1},{12},{13},{14}," +
                "{15:F1},{16},{17},{18:F1},{19:F1},{20:F2},{21},{22},{23:F3},{24:F1},{25:F1},\"{26}\"",
                Time.time, Time.unscaledTime, kind, source.Owner, source.EntityId, orders.Current, state,
                orders.Target == null ? 0L : orders.Target.Id, destination.x, destination.z, position.x, position.z,
                movement.IsMoving ? 1 : 0, radar.Enemies.Count, preferred == null ? 0L : preferred.Id,
                distance, preferred != null && ShipEngagement.CanEngage(preferred, movement, weapon) ? 1 : 0,
                tracked == null ? 0L : tracked.Id, source.RadarRange, weapon.AttackDistance,
                caster.Modifiers.VisionMultiplier, caster.Modifiers.IsIonDisabled ? 1 : 0,
                caster.Modifiers.IsCloaked ? 1 : 0, facingDot, caster.Health.Hull, caster.Health.Shields,
                reason.Replace("\"", "\"\"")));
            if (kind == "release") _nextSample.Remove(orders);
            if (Time.unscaledTime >= _nextFlush) Flush();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void Flush()
        {
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            if (_lines.Length == 0) return;
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            if (!File.Exists(_path)) File.WriteAllText(_path, HEADER + System.Environment.NewLine);
            File.AppendAllText(_path, _lines.ToString());
            _lines.Clear();
        }
#endif
    }
}
