using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor
{
    internal static class RenderAuditRenderingDebuggerTool
    {
        [CliCommand("render_audit_rendering_debugger", "Captures current Rendering Debugger panel values to Logs/RenderAudit.")]
        public static object CaptureRenderingDebugger()
        {
            var directory = RenderAuditCaptureStatus.CreateCaptureDirectory("rendering-debugger");
            return Capture(directory, "standalone");
        }

        internal static Dictionary<string, object> Capture(string directory, string phase)
        {
            var result = RenderAuditCaptureStatus.CreateResult("completed", "Captured registered Rendering Debugger panel values.");
            result["phase"] = phase;
            var panels = new List<object>();

            foreach (var panel in DebugManager.instance.panels)
            {
                var panelValues = new List<object>();
                CaptureWidgets(panel, panel.displayName, panelValues);
                panels.Add(new Dictionary<string, object>
                {
                    ["display_name"] = panel.displayName,
                    ["values"] = panelValues
                });
            }

            result["panels"] = panels;
            result["panel_count"] = panels.Count;
            RenderAuditCaptureStatus.WriteJson(directory, "rendering-debugger.json", result);
            result.Remove("panels");
            result["artifact"] = "rendering-debugger.json";
            return result;
        }

        private static void CaptureWidgets(DebugUI.IContainer container, string parentPath, List<object> values)
        {
            for (var index = 0; index < container.children.Count; index++)
            {
                var widget = container.children[index];
                var label = string.IsNullOrWhiteSpace(widget.displayName) ? $"{widget.GetType().Name}[{index}]" : widget.displayName;
                var path = $"{parentPath}/{label}";
                if (widget is DebugUI.IContainer childContainer)
                {
                    CaptureWidgets(childContainer, path, values);
                    continue;
                }

                if (widget is DebugUI.ValueTuple valueTuple)
                {
                    for (var valueIndex = 0; valueIndex < valueTuple.values.Length; valueIndex++)
                    {
                        CaptureValue(valueTuple.values[valueIndex], $"{path}/Value[{valueIndex}]", values);
                    }

                    continue;
                }

                CaptureValue(widget, path, values);
            }
        }

        private static void CaptureValue(DebugUI.Widget widget, string fallbackPath, List<object> values)
        {
            if (widget is not DebugUI.IValueField && widget is not DebugUI.Value)
            {
                return;
            }

            var valueEntry = new Dictionary<string, object>
            {
                ["path"] = string.IsNullOrWhiteSpace(widget.queryPath) ? fallbackPath : widget.queryPath,
                ["display_name"] = string.IsNullOrWhiteSpace(widget.displayName) ? fallbackPath : widget.displayName,
                ["widget_type"] = widget.GetType().FullName
            };

            try
            {
                var value = widget is DebugUI.IValueField valueField
                    ? valueField.GetValue()
                    : ((DebugUI.Value)widget).GetValue();
                valueEntry["value_type"] = value?.GetType().FullName;
                valueEntry["value"] = SerializeValue(value);
            }
            catch (Exception exception)
            {
                valueEntry["value_error"] = exception.Message;
            }

            values.Add(valueEntry);
        }

        private static object SerializeValue(object value)
        {
            if (value == null || value is string || value is bool)
            {
                return value;
            }

            if (value is char || value is Enum)
            {
                return value.ToString();
            }

            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            {
                return value;
            }

            if (value is UnityEngine.Object unityObject)
            {
                return new Dictionary<string, object>
                {
                    ["name"] = unityObject.name,
                    ["instance_id"] = unityObject.GetInstanceID()
                };
            }

            return value.ToString();
        }
    }
}
