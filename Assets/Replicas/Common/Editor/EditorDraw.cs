using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Common.EditorTools
{
    /// <summary>Small IMGUI drawing helpers shared by the level editors.</summary>
    public static class EditorDraw
    {
        // Inset outline drawn as four thin rects.
        public static void Outline(Rect rect, Color color, float thickness = 1f)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static bool ColorButton(string label, Color color, params GUILayoutOption[] options)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(label, options);
            GUI.backgroundColor = prev;
            return clicked;
        }

        public static void Banner(string text, Color background)
        {
            var rect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(rect, background);
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 12
            };
            GUI.Label(rect, text, style);
        }
    }
}
