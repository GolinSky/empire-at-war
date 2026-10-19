using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    [EditorToolInfo("Open a ShipView prefab, then add, remove, mirror and place hardpoints on the hull or on ALO HP_ bones. " +
        "Keeps ShipUnits, WeaponComponent, Injectables and IDs in sync; every action is undoable.",
        typeof(HardPointPlacementWindow))]
    public sealed class HardPointPlacementWindow : EditorWindow
    {
        private const string MENU_PATH = EditorToolCatalog.MENU_ROOT + "Units/Ships/Hardpoint Placement";
        private const int NEW_WEAPON = 0;
        private const int NEW_HARD_POINT = 1;

        private EntityBindings _bindings;
        private ShipHardPointRegistry _registry;
        private List<HardPoint> _templates = new List<HardPoint>();
        private List<Transform> _bones = new List<Transform>();
        private List<Transform> _freeBones = new List<Transform>();
        private List<string> _issues = new List<string>();
        private Vector2 _scroll;
        private int _templateIndex;
        private WeaponType _newWeaponType;
        private HardPointType _newHardPointType;
        private bool _placing;
        private bool _mirror;
        private bool _showLabels = true;
        private bool _showArcs = true;
        private bool _showBones = true;

        [MenuItem(MENU_PATH)]
        public static void Open() => GetWindow<HardPointPlacementWindow>("Hardpoints");

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyChanged += Refresh;
            Undo.undoRedoPerformed += Refresh;
            PrefabStage.prefabStageOpened += OnStageChanged;
            PrefabStage.prefabStageClosing += OnStageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.hierarchyChanged -= Refresh;
            Undo.undoRedoPerformed -= Refresh;
            PrefabStage.prefabStageOpened -= OnStageChanged;
            PrefabStage.prefabStageClosing -= OnStageChanged;
        }

        private void OnStageChanged(PrefabStage stage) => EditorApplication.delayCall += Refresh;

        private void Refresh()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            _bindings = stage == null ? null : stage.prefabContentsRoot.GetComponent<EntityBindings>();
            if (_bindings == null || _bindings.Health.Length != 1)
            {
                _bindings = null;
                _registry = null;
                Repaint();
                return;
            }
            _registry = new ShipHardPointRegistry(_bindings);
            _templates = HardPointNaming.GetTemplates(_registry.HardPoints);
            _templateIndex = Mathf.Min(_templateIndex, _templates.Count + 1);
            _bones = HardPointBoneScanner.FindBones(_registry.Root);
            _freeBones = HardPointBoneScanner.FindFreeBones(_registry.Root, _registry.HardPoints);
            _issues = ShipHardPointValidator.Validate(_registry, _bindings,
                ShipHardPointValidator.FindShipData(stage.assetPath));
            Repaint();
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            if (_registry == null)
            {
                EditorGUILayout.HelpBox("Open a ShipView prefab (root with EntityBindings and one HealthComponent).",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawDisplayOptions();
            DrawAddPanel();
            DrawBonePanel();
            DrawHardPointList();
            DrawValidation();
            EditorGUILayout.EndScrollView();
        }

        private void DrawDisplayOptions()
        {
            EditorGUILayout.LabelField(_bindings.name, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _showLabels = GUILayout.Toggle(_showLabels, "Labels", EditorStyles.miniButtonLeft);
                _showArcs = GUILayout.Toggle(_showArcs, "Arcs", EditorStyles.miniButtonMid);
                _showBones = GUILayout.Toggle(_showBones, "ALO Bones", EditorStyles.miniButtonMid);
                _mirror = GUILayout.Toggle(_mirror, "Mirror X", EditorStyles.miniButtonRight);
            }
            if (GUI.changed) SceneView.RepaintAll();
        }

        private void DrawAddPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Add", EditorStyles.boldLabel);
            string[] options = new[] { "New Weapon Hardpoint", "New Hardpoint" }
                .Concat(_templates.Select(template => $"Clone: {HardPointNaming.GetGroup(template)} ({Describe(template)})"))
                .ToArray();
            _templateIndex = Mathf.Clamp(EditorGUILayout.Popup("Template", _templateIndex, options), 0, options.Length - 1);
            if (_templateIndex == NEW_WEAPON) _newWeaponType = (WeaponType)EditorGUILayout.EnumPopup("Weapon", _newWeaponType);
            if (_templateIndex <= NEW_HARD_POINT)
                _newHardPointType = (HardPointType)EditorGUILayout.EnumPopup("Type", _newHardPointType);

            Color previous = GUI.backgroundColor;
            if (_placing) GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            _placing = GUILayout.Toggle(_placing, _placing ? "Placing — click the hull (toggle off to stop)" : "Place on Hull",
                "Button", GUILayout.Height(24));
            GUI.backgroundColor = previous;
        }

        private void DrawBonePanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Free ALO bones ({_freeBones.Count} of {_bones.Count})", EditorStyles.boldLabel);
            if (_bones.Count == 0)
            {
                EditorGUILayout.HelpBox("This model has no HP_ bones; place hardpoints on the hull instead.", MessageType.None);
                return;
            }
            foreach (Transform bone in _freeBones.ToList())
            {
                HardPoint template = HardPointBoneScanner.FindTemplate(bone.name, _registry.HardPoints);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(bone.name, GUILayout.Width(120));
                    EditorGUILayout.LabelField(template == null ? $"→ selected template / {HardPointBoneScanner.GuessType(bone.name)}"
                        : $"→ {HardPointNaming.GetGroup(template)}", EditorStyles.miniLabel);
                    if (GUILayout.Button("Frame", EditorStyles.miniButtonLeft, GUILayout.Width(44))) Frame(bone);
                    if (GUILayout.Button("Add", EditorStyles.miniButtonRight, GUILayout.Width(36))) Run(() => AddFromBone(bone));
                }
            }
            if (_freeBones.Count > 1 && GUILayout.Button($"Add All {_freeBones.Count}"))
            {
                Run(() => _freeBones.ToList().ForEach(AddFromBone));
            }
        }

        private void DrawHardPointList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Hardpoints ({_registry.HardPoints.Count})", EditorStyles.boldLabel);
            foreach (HardPoint hardPoint in _registry.HardPoints.ToList())
            {
                if (hardPoint == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.color = HardPointSceneDrawer.GetColor(hardPoint);
                    EditorGUILayout.LabelField($"{hardPoint.Id}", GUILayout.Width(26));
                    GUI.color = Color.white;
                    if (GUILayout.Button(hardPoint.name, EditorStyles.label)) Select(hardPoint);
                    EditorGUILayout.LabelField(Describe(hardPoint), EditorStyles.miniLabel, GUILayout.Width(150));
                    Transform bone = HardPointBoneScanner.FindBone(hardPoint, _bones);
                    using (new EditorGUI.DisabledScope(bone == null))
                    {
                        if (GUILayout.Button(new GUIContent("Bone", "Snap to its ALO bone"), EditorStyles.miniButtonLeft,
                                GUILayout.Width(40))) Run(() => SnapToBone(hardPoint, bone));
                    }
                    if (GUILayout.Button(new GUIContent("Mirror", "Clone onto the other side with a flipped arc"),
                            EditorStyles.miniButtonMid, GUILayout.Width(46)))
                        Run(() => Select(_registry.AddMirror(hardPoint,
                            HardPointNaming.GetNextName(HardPointNaming.GetGroup(hardPoint), _registry.HardPoints))));
                    using (new EditorGUI.DisabledScope(_registry.IsHangarReference(hardPoint)))
                    {
                        if (GUILayout.Button(new GUIContent("✕", "Remove (reassign the hangar first if disabled)"),
                                EditorStyles.miniButtonRight, GUILayout.Width(24))) Run(() => _registry.Remove(hardPoint));
                    }
                }
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (_issues.Count == 0) EditorGUILayout.HelpBox("All hardpoint lists are in sync.", MessageType.Info);
            foreach (string issue in _issues) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Renumber IDs")) Run(_registry.Renumber);
                if (GUILayout.Button("Register Unlisted"))
                    Run(() => ShipHardPointValidator.FindUnregistered(_registry).ForEach(_registry.Register));
                if (GUILayout.Button(new GUIContent("Bake AI Loadouts", "Save the prefab first; reads saved ShipView assets.")))
                    Run(WeaponLoadoutBaker.BakeAll);
            }
        }

        /// <summary>Applies a change from the window, then rebuilds the lists and ends this IMGUI pass.</summary>
        private void Run(System.Action action)
        {
            action();
            Refresh();
            GUIUtility.ExitGUI();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_registry == null || _registry.Root == null) return;
            HardPointSceneDrawer.DrawHardPoints(_registry.HardPoints, _showLabels, _showArcs, Select);
            if (_showBones) HardPointSceneDrawer.DrawBones(_freeBones, _showLabels, bone => { AddFromBone(bone); Refresh(); });
            if (_placing && HardPointSceneDrawer.TryPlace(sceneView, out Vector3 point))
            {
                AddAt(point);
                Refresh();
            }
        }

        private void AddAt(Vector3 point)
        {
            HardPoint added;
            if (_templateIndex <= NEW_HARD_POINT)
            {
                string group = _templateIndex == NEW_WEAPON ? _newWeaponType.ToString() : _newHardPointType.ToString();
                WeaponType? weaponType = _templateIndex == NEW_WEAPON ? _newWeaponType : (WeaponType?)null;
                Transform parent = _registry.HardPoints.Count > 0 ? _registry.HardPoints[0].transform.parent : _registry.Root;
                added = _registry.AddNew(_newHardPointType, weaponType, parent,
                    HardPointNaming.GetNextName(group, _registry.HardPoints), point);
            }
            else
            {
                HardPoint template = _templates[_templateIndex - 2];
                added = _registry.AddClone(template,
                    HardPointNaming.GetNextName(HardPointNaming.GetGroup(template), _registry.HardPoints), point);
            }
            if (_mirror)
                _registry.AddMirror(added, HardPointNaming.GetNextName(HardPointNaming.GetGroup(added), _registry.HardPoints));
            Select(added);
        }

        private void AddFromBone(Transform bone)
        {
            HardPoint template = HardPointBoneScanner.FindTemplate(bone.name, _registry.HardPoints);
            if (template == null && _templateIndex > NEW_HARD_POINT) template = _templates[_templateIndex - 2];
            HardPoint added = template == null
                ? _registry.AddNew(HardPointBoneScanner.GuessType(bone.name),
                    HardPointBoneScanner.GuessType(bone.name) == HardPointType.Weapon ? _newWeaponType : (WeaponType?)null,
                    _registry.HardPoints.Count > 0 ? _registry.HardPoints[0].transform.parent : _registry.Root,
                    bone.name, bone.position)
                : _registry.AddClone(template, HardPointNaming.ForBone(template, bone), bone.position);
            Select(added);
        }

        private static void SnapToBone(HardPoint hardPoint, Transform bone)
        {
            Undo.RecordObject(hardPoint.transform, "Snap Hardpoint to Bone");
            hardPoint.transform.position = bone.position;
        }

        private static void Select(HardPoint hardPoint)
        {
            Selection.activeGameObject = hardPoint.gameObject;
            EditorGUIUtility.PingObject(hardPoint.gameObject);
        }

        private static void Frame(Transform target)
        {
            Selection.activeTransform = target;
            SceneView.lastActiveSceneView.FrameSelected();
        }

        private static string Describe(HardPoint hardPoint) =>
            hardPoint is WeaponHardPoint weapon ? $"{weapon.WeaponType}" : $"{hardPoint.HardPointType}";
    }
}
