# Utilities

## Placement

| Folder | Types | Responsibility |
| --- | --- | --- |
| `Attributes/` | `SubclassSelectorAttribute` | Inspector annotation |
| `Collections/` | `NativeArrayBuffer` | Persistent native buffer capacity |
| `Conversion/` | `NumericsConversionExtensions` | Unity → System.Numerics vectors; bidirectional Vector3/quaternion conversion |
| `Math/` | `PlanarGeometry`, `Vector2Range`, `VelocitySmoothing` | XZ geometry, rectangular bounds, velocity changes |
| `Notifications/` | `ReplayNotifier` | Observer notification with initial state replay |
| `Random/` | `RandomValue`, `RandomFloat`, `RandomFromArray`, `RandomAudioClips` | Serialized random ranges and array selection |
| `Tweening/` | `SequenceExtensions` | Optional sequence cancellation |

- General helpers belong here when their behavior is shared across systems.
- Helpers tied to a gameplay feature remain beside that feature, even when static.
- Existing namespaces and serialized field names are preserved when moving scripts.
- Move scripts with `AssetDatabase.MoveAsset` to retain GUIDs and prefab/scene bindings.

## Consolidated duplication

| Previous implementations | Shared implementation |
| --- | --- |
| `MapGeometry` distance, center, reflection, polyline distance | `PlanarGeometry`; `MapGeometry` removed and callers migrated |
| `OwnedCircle.Contains`, capture-site and reinforcement-zone presenter/system circle checks | `PlanarGeometry.ContainsCircle`; callers supply radius plus their existing clearance |
| `ShipBezierPath.GetPlanarDirection`, `ShipRoutePlanner.GetPlanarDirection` | `PlanarGeometry.NormalizeDirection` |
| `ReinforcementZonesSystem` height flattening, exit-point and circle-bounds math | `PlanarGeometry.Flatten`, `GetPointTowards`, `GetCircleBounds` |
| `PointerGestures.ToNumerics(Vector2)` | `NumericsConversionExtensions.ToNumerics(Vector2)` |

- Geometry uses XZ and ignores height. Circle boundaries remain inclusive.
- Direction normalization flattens the requested direction, then the fallback; two zero/vertical vectors → `Vector3.forward`.
- Polyline distance with fewer than two points remains `float.MaxValue`; a zero-length segment uses its endpoint.
- `Vector2Range.IsInRange` retains strict boundaries; `Clamp` retains inclusive clamping.

## Utilities beside their owners

| Location under `Assets/Scripts` | Types | Why they remain local |
| --- | --- | --- |
| `Components/Movement/Formation/` | `FormationConversion`, `FormationModel`, `BattleFormationModel` | Formation coordinates and placement rules |
| `Components/Squadron/Flight/` | `SquadronFormation`; static methods in `FighterKinematics`, `FighterManeuver` | Fighter pose and maneuver rules using System.Numerics |
| `Components/Ship/Movement/` | `ShipRotationKinematics`, `ShipAvoidancePlanner`, `ShipBezierPath` | Ship movement and obstacle rules |
| `Services/ShipNavigation/` | `ShipRoutePlanner` | Navigation planning; generic direction math extracted |
| `Components/Selection/Marquee/` | `MarqueeSelectionUtility` | Marquee-specific rectangle and point types |
| `Components/Weapon/` | `WeaponTargetSelector`, `AttackSequenceDiagnostics` | Weapon aim rules and counters |
| `Components/Health/` | `ShipClassExtensions`, `HardPointOverlayGraphics` | Ship class and overlay behavior |
| `Services/Cheats/` | `DebugRangeCircle`, `DebugRangeCircleFactory` | Selection-gated range debug rendering; moved out of Utils |
| `Components/ViewComponents/Rendering/` | `DrawCircle` | Serialized fixed-circle component; moved out of Utils |
| `Components/Ui/Layout/` | `SafeAreaHelper` | UI safe-area component; moved out of Utils |
| `Services/Camera/` | `CinematicShotSolver` | Camera framing, constraints, and poses |
| `Services/Input/` | `BindingSlotCatalog`, `BindingConflicts` | Input binding identifiers and conflict rules |
| `Services/Timing/` | `BattleProfilerMarkers`, `BattlePerformanceCaptureReport` | Battle profiling and reporting |
| `Entities/BaseEntity/`, `Entities/Units/` | `EntityDetection`, `EntityRoles` | Entity facade queries |
| `Entities/Players/` | `MatchRules` | Match roster validation |
| `Entities/FogOfWar/` | `FogVisibilityModel` | Fog reveal rules |
| `Entities/Ship/StateMachine/` | `ShipEngagement` | Ship engagement and pursuit rules |
| `Entities/EnemyFaction/Models/` | `ResponseCurve`, `IntelConfidence`, `CombatMatchup` | AI scoring, intelligence aging, combat forecasts |
| `Entities/Tooltip/` | `UnitTooltipContent`, `ShipAbilityTooltipContent`, `EntityTooltipContent` | Domain-specific tooltip content |

## Similar code with different behavior

- `DrawCircle` creates `segments + 1` points at the Transform's height; `DebugRangeCircle` uses 96 local points with renderer looping and changes radius on demand. Their lifecycles and rendering contracts differ.
- `SequenceExtensions.KillExt` cancels an optional sequence and reports whether it existed. Package `DotweenExtensions.KillIfExist` also checks active/playing state; these contracts are distinct. `DotweenExtensions_v2` renamed to `SequenceExtensions`.
- `RandomFloat` uses Unity's random state; `RandomFromArray` owns a System.Random instance. `RandomAudioClips` is a serialized concrete specialization, not a second selection algorithm.
- `AttackMoveEngagement.IsAlive` excludes cloaked entities; `SquadronTargetSelector.IsAlive` permits cloaked entities and handles absent targets. Keep those policies separate.
- Unity/System.Numerics/Unity.Mathematics geometry stays in its existing execution domain. Pure models and Burst jobs must not acquire UnityEngine dependencies just to share arithmetic.
- Bezier evaluation in `CubicBezierSegment` supplies tangents and integrates with route arc lengths. Package `PathCalculationUtils` samples positions; replacing route behavior with that helper would lose its contract.

## Audit scope

- Reviewed `Utils` and searched all project-owned runtime C# in `BaseSystem`, `Components`, `Entities`, and `Services` for static classes, static methods, utility/helper types, conversions, and geometry calculations.
- Runtime inventory after consolidation: 39 static classes; 221 non-builder static method candidates.
- Excluded Editor tools, tests, external packages, and builder/factory/binding methods from the duplicate-method scan.
- Remaining static builders/bindings stay local: `InstallerExtensions`, `EntityFeatureBindings`, `TooltipBindings`, `FieldVolumeCover`, `UnitCombatProfileFactory`.
- A text scan normalizing whitespace and parameter/local names found the duplicate direction helpers. After extraction it found no further identical candidates with normalized bodies of at least 50 characters. This is candidate detection, not proof that all semantic duplication is absent.

## Verification (2026-10-10)

- Unity completed compilation and importing with no compiler/import errors; the merged and renamed classes were verified in the loaded assembly.
- All 13 moved scripts retained their GUIDs; the 10 scripts changed only by relocation retained identical source contents.
- 10 geometry regression tests passed; movement suite: 47/47; isolated map generation suite: 17/17; selection input suite: 2/2.
- Broader `EmpireAtWar.Tests.Editor` run: 817/842 passed. Two map tests were interrupted by bridge timeout logs and passed in the isolated rerun.
- Other 23 failures concern untouched ship stats, tiers, prefab/rendering/loadout/wreck data, enemy production expectations, and engine-hardpoint null references. They were not repaired in this utility refactor.
