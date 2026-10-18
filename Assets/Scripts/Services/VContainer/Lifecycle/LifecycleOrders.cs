namespace EmpireAtWar.Lifecycle
{
    /// <summary>Every <see cref="LifecycleOrderAttribute"/> value in one place. Unlisted callbacks run at 0 in
    /// registration order. Initialize and the tick phases run ascending; Dispose and LateDispose run descending.</summary>
    public static class LifecycleOrders
    {
        public const int INPUT_LOCK_INITIALIZE = -400;
        public const int PLAYER_SCOPES_INITIALIZE = -300;
        public const int CORE_HUD_INITIALIZE = -200;
        public const int HEALTH_INITIALIZE = -100;
        public const int ORDER_INPUT_INITIALIZE = -100;
        public const int SQUADRON_FLIGHT_INITIALIZE = HEALTH_INITIALIZE + 10;
        public const int HUD_PRESENTERS_INITIALIZE = 100;

        public const int SHIP_SFX_TICK = 1000;

        public const int FIGHTER_POSE_LATE_TICK = -2000;
        public const int COMBAT_LATE_TICK = -1000;
        public const int SHIP_SFX_LATE_TICK = 1000;
        public const int NAVIGATION_LATE_TICK = 2000;

        public const int ENTITY_SCOPES_DISPOSE = 1000;
        public const int PLAYER_SCOPES_DISPOSE = 300;
        public const int PLANETS_DISPOSE = 200;
        public const int SHIP_SFX_DISPOSE = -1000;

        public const int CAPTURE_SITE_BUILD_UI_LATE_DISPOSE = 100;
        public const int INPUT_LOCK_LATE_DISPOSE = -2000;
    }
}
