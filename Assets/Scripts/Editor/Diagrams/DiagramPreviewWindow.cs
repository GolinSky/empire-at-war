using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Diagrams
{
    // Lists Logs/Diagrams/* and shows the rendered PNG with zoom (slider or Ctrl+wheel) and drag-to-pan.
    public sealed class DiagramPreviewWindow : EditorWindow
    {
        private const string MENU_PATH = "Tools/Empire At War/Workflow/Diagrams/Diagram Preview";
        private const float MIN_ZOOM = 0.1f;
        private const float MAX_ZOOM = 4f;
        private const float WHEEL_ZOOM_STEP = 0.1f;

        [SerializeField] private string selectedName;
        [SerializeField] private float zoom = 1f;

        private ListView _list;
        private Label _status;
        private Slider _zoomSlider;
        private ScrollView _viewport;
        private Image _image;
        private Texture2D _texture;
        private Vector2 _dragStart;
        private Vector2 _scrollStart;
        private bool _dragging;

        private readonly List<string> _names = new List<string>();

        [MenuItem(MENU_PATH)]
        [EditorToolInfo("Browse diagrams rendered by the diagram_generate / diagram_render CLI commands (Logs/Diagrams).")]
        private static void OpenMenu() => EditorHubWindow.OpenTool(MENU_PATH);

        public static void ShowDiagram(string name)
        {
            var window = GetWindow<DiagramPreviewWindow>("Diagram Preview");
            window.selectedName = name;
            if (window._list != null)
            {
                window.Refresh();
                window.Select(name);
            }
            window.Show();
            window.Focus();
        }

        private void OnDisable() => ReleaseTexture();

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, flexShrink = 0 } };
            toolbar.Add(new Button(Refresh) { text = "Refresh" });
            toolbar.Add(new Button(() => OpenSelected(DiagramOpenTarget.Rider)) { text = "Open in Rider" });
            toolbar.Add(new Button(() => OpenSelected(DiagramOpenTarget.Browser)) { text = "Open in Browser" });
            toolbar.Add(new Button(RevealSelected) { text = "Reveal" });
            _zoomSlider = new Slider("Zoom", MIN_ZOOM, MAX_ZOOM) { value = zoom, style = { flexGrow = 1 } };
            _zoomSlider.RegisterValueChangedCallback(change => SetZoom(change.newValue));
            toolbar.Add(_zoomSlider);
            root.Add(toolbar);

            var split = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };
            _list = new ListView(_names, 20, () => new Label(), (element, index) => ((Label)element).text = _names[index])
            {
                selectionType = SelectionType.Single
            };
            _list.selectionChanged += selection =>
            {
                foreach (object item in selection) LoadDiagram((string)item);
            };
            split.Add(_list);

            _viewport = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { style = { flexGrow = 1 } };
            _image = new Image { scaleMode = ScaleMode.StretchToFill };
            _viewport.Add(_image);
            _viewport.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
            split.Add(_viewport);
            root.Add(split);

            _status = new Label { style = { flexShrink = 0 } };
            root.Add(_status);

            Refresh();
            if (_names.Contains(selectedName)) Select(selectedName);
        }

        private void Refresh()
        {
            _names.Clear();
            _names.AddRange(DiagramOutputPaths.ListNames());
            _list.RefreshItems();
            _status.text = _names.Count == 0
                ? $"No diagrams in {DiagramOutputPaths.ROOT}. Run: unity command diagram_generate --paths <folder> --name <name>"
                : $"{_names.Count} diagram(s) in {DiagramOutputPaths.ROOT}.";
        }

        private void Select(string name)
        {
            int index = _names.IndexOf(name);
            if (index < 0)
                throw new FileNotFoundException($"No diagram named '{name}' under {DiagramOutputPaths.ROOT}.");
            _list.SetSelection(index);
            LoadDiagram(name);
        }

        private void LoadDiagram(string name)
        {
            selectedName = name;
            string png = DiagramOutputPaths.Png(name);
            ReleaseTexture();
            if (!File.Exists(png))
            {
                _image.image = null;
                _status.text = $"{name}: not rendered yet. Run: unity command diagram_render --puml {DiagramOutputPaths.Puml(name)}";
                return;
            }

            _texture = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            _texture.LoadImage(File.ReadAllBytes(png));
            _image.image = _texture;
            ApplyZoom();
            _status.text = $"{name}: {_texture.width}×{_texture.height} px — {png}";
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, MIN_ZOOM, MAX_ZOOM);
            _zoomSlider.SetValueWithoutNotify(zoom);
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_texture == null) return;
            _image.style.width = _texture.width * zoom;
            _image.style.height = _texture.height * zoom;
        }

        private void OnWheel(WheelEvent evt)
        {
            if (!evt.ctrlKey) return;
            SetZoom(zoom - Mathf.Sign(evt.delta.y) * WHEEL_ZOOM_STEP);
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _dragging = true;
            _dragStart = evt.position;
            _scrollStart = _viewport.scrollOffset;
            _viewport.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging) return;
            _viewport.scrollOffset = _scrollStart - ((Vector2)evt.position - _dragStart);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _dragging = false;
            _viewport.ReleasePointer(evt.pointerId);
        }

        private void OpenSelected(DiagramOpenTarget target)
        {
            if (string.IsNullOrEmpty(selectedName)) return;
            DiagramOpener.Open(selectedName, target);
        }

        private void RevealSelected()
        {
            if (string.IsNullOrEmpty(selectedName)) return;
            EditorUtility.RevealInFinder(DiagramOutputPaths.Puml(selectedName));
        }

        private void ReleaseTexture()
        {
            if (_texture == null) return;
            DestroyImmediate(_texture);
            _texture = null;
        }
    }
}
