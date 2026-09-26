using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Common.EditorTools
{
    /// <summary>
    /// Level-asset toolbar shared by the level editors: discovers every asset of type T, lets the
    /// designer pick / create / refresh / ping one, and guards unsaved work before switching.
    /// </summary>
    public sealed class LevelAssetBrowser<T> where T : ScriptableObject
    {
        private readonly List<T> _levels = new();
        private readonly string _defaultFolder;
        private readonly string _defaultName;
        private readonly Func<bool> _isDirty;
        private readonly Action<T> _onSelected;
        private readonly Action<T> _initializeNew;

        private string[] _levelNames = Array.Empty<string>();
        private int _selectedIndex;

        public T Current { get; private set; }

        /// <param name="isDirty">True when the working copy has unsaved edits.</param>
        /// <param name="onSelected">Loads the chosen asset into the working copy.</param>
        /// <param name="initializeNew">Fills a freshly created asset before it is saved to disk.</param>
        public LevelAssetBrowser(string defaultFolder, string defaultName, Func<bool> isDirty,
                                 Action<T> onSelected, Action<T> initializeNew)
        {
            _defaultFolder = defaultFolder;
            _defaultName = defaultName;
            _isDirty = isDirty;
            _onSelected = onSelected;
            _initializeNew = initializeNew;
        }

        public void Refresh()
        {
            string previousPath = Current != null ? AssetDatabase.GetAssetPath(Current) : null;

            _levels.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var level = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null)
                    _levels.Add(level);
            }

            _levels.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            _levelNames = new string[_levels.Count];
            for (int i = 0; i < _levels.Count; i++)
                _levelNames[i] = _levels[i].name;

            if (_levels.Count == 0)
            {
                Current = null;
                _selectedIndex = 0;
                return;
            }

            int restored = previousPath != null
                ? _levels.FindIndex(l => AssetDatabase.GetAssetPath(l) == previousPath)
                : -1;
            Select(restored >= 0 ? restored : 0, force: true);
        }

        public void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Level:", EditorStyles.miniLabel, GUILayout.Width(40f));
            int picked = EditorGUILayout.Popup(_selectedIndex, _levelNames, EditorStyles.toolbarPopup, GUILayout.Width(170f));
            if (picked != _selectedIndex)
                Select(picked);

            if (GUILayout.Button("New Level", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                PromptCreate();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                Refresh();

            GUILayout.FlexibleSpace();

            if (_isDirty())
                GUILayout.Label("● Unsaved", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(Current == null))
                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    EditorGUIUtility.PingObject(Current);

            EditorGUILayout.EndHorizontal();
        }

        private void Select(int index, bool force = false)
        {
            index = Mathf.Clamp(index, 0, _levels.Count - 1);
            if (!force && index == _selectedIndex)
                return;

            if (_isDirty() && !ConfirmDiscard())
                return;

            _selectedIndex = index;
            Current = _levels[index];
            _onSelected(Current);
        }

        private void PromptCreate()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Level", _defaultName, "asset",
                "Choose a name and location for the new level.", _defaultFolder);
            if (string.IsNullOrEmpty(path))
                return;

            Create(path);
            GUIUtility.ExitGUI();
        }

        private void Create(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var level = ScriptableObject.CreateInstance<T>();
            _initializeNew(level);
            AssetDatabase.CreateAsset(level, assetPath);
            AssetDatabase.SaveAssets();

            Refresh();
            int index = _levels.IndexOf(level);
            if (index >= 0)
                Select(index, force: true);
        }

        private static bool ConfirmDiscard() => EditorUtility.DisplayDialog(
            "Unsaved Changes",
            "You have unsaved changes. Discard them?",
            "Discard", "Keep Editing");
    }
}
