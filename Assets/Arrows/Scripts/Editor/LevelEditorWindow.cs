using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Arrows.EditorTools
{
    public class LevelEditorWindow : EditorWindow
    {
        private const float CellSize = 40f;
        private const string DefaultFolder = "Assets/Arrows/Levels";

        // Discovered LevelData assets; one is active at a time.
        private readonly List<LevelData> _levels = new();
        private string[] _levelNames = new string[0];
        private int _selectedIndex;
        private LevelData _level;

        // In-memory working buffer. Only written back to the asset on a validated Save.
        private int _width = 5;
        private int _height = 5;
        private readonly List<HeadData> _heads = new();
        private bool _dirty;

        // Paint state.
        private Direction _brush = Direction.Up;
        private bool _erase;
        private int _headCount = 5;

        private readonly BoardLogic _boardLogic = new();
        private HashSet<Vector2Int> _violationCoords = new();

        [MenuItem("Window/Arrows/Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable() => RefreshLevels();

        // ---- Asset discovery ------------------------------------------------

        private void RefreshLevels()
        {
            string previousPath = _level != null ? AssetDatabase.GetAssetPath(_level) : null;

            _levels.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:LevelData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                    _levels.Add(level);
            }

            _levels.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            _levelNames = _levels.Select(l => l.name).ToArray();

            if (_levels.Count == 0)
            {
                _level = null;
                _selectedIndex = 0;
                return;
            }

            int restored = previousPath != null
                ? _levels.FindIndex(l => AssetDatabase.GetAssetPath(l) == previousPath)
                : -1;
            SelectLevel(restored >= 0 ? restored : 0, force: true);
        }

        private void SelectLevel(int index, bool force = false)
        {
            index = Mathf.Clamp(index, 0, _levels.Count - 1);
            if (!force && index == _selectedIndex)
                return;

            if (_dirty && !ConfirmDiscard())
                return;

            _selectedIndex = index;
            _level = _levels[index];
            LoadWorkingCopy(_level);
        }

        private void LoadWorkingCopy(LevelData level)
        {
            _width = Mathf.Max(1, level.width);
            _height = Mathf.Max(1, level.height);
            _heads.Clear();
            _heads.AddRange(level.heads);
            _dirty = false;
        }

        private bool ConfirmDiscard() => EditorUtility.DisplayDialog(
            "Unsaved Changes",
            "You have unsaved changes. Discard them?",
            "Discard", "Keep Editing");

        private void CreateLevel(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var level = CreateInstance<LevelData>();
            level.width = _width;
            level.height = _height;
            AssetDatabase.CreateAsset(level, assetPath);
            AssetDatabase.SaveAssets();

            RefreshLevels();
            int idx = _levels.IndexOf(level);
            if (idx >= 0)
                SelectLevel(idx, force: true);
        }

        // ---- GUI ------------------------------------------------------------

        private void OnGUI()
        {
            DrawToolbar();

            if (_level == null)
            {
                EditorGUILayout.HelpBox("No LevelData asset selected. Create one to begin.", MessageType.Info);
                return;
            }

            HandleShortcuts();

            DrawDimensions();
            DrawPalette();
            DrawRandomizer();

            EditorGUILayout.Space();
            RevalidateForDisplay();
            DrawGrid();

            EditorGUILayout.Space();
            DrawSaveRow();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Level:", EditorStyles.miniLabel, GUILayout.Width(40f));
            int picked = EditorGUILayout.Popup(_selectedIndex, _levelNames,
                EditorStyles.toolbarPopup, GUILayout.Width(160f));
            if (picked != _selectedIndex)
                SelectLevel(picked);

            if (GUILayout.Button("New Level", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Level", "Level", "asset",
                    "Choose a name and location for the new level.", DefaultFolder);
                if (!string.IsNullOrEmpty(path))
                {
                    CreateLevel(path);
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                RefreshLevels();

            GUILayout.FlexibleSpace();

            if (_dirty)
                GUILayout.Label("● Unsaved", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(_level == null))
                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    EditorGUIUtility.PingObject(_level);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDimensions()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Width", GUILayout.Width(45f));
            int w = Mathf.Max(1, EditorGUILayout.IntField(_width, GUILayout.Width(50f)));
            GUILayout.Space(12f);
            GUILayout.Label("Height", GUILayout.Width(50f));
            int h = Mathf.Max(1, EditorGUILayout.IntField(_height, GUILayout.Width(50f)));

            if (EditorGUI.EndChangeCheck())
                Resize(w, h);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void Resize(int w, int h)
        {
            // Dropping heads that fall outside the new bounds needs confirmation.
            bool orphans = _heads.Any(head =>
                head.coordinates.x >= w || head.coordinates.y >= h);
            if (orphans && !EditorUtility.DisplayDialog("Resize Board",
                    "Shrinking will remove heads outside the new bounds. Continue?",
                    "Remove", "Cancel"))
                return;

            _width = w;
            _height = h;
            _heads.RemoveAll(head => head.coordinates.x >= w || head.coordinates.y >= h);
            MarkDirty();
        }

        private void DrawPalette()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Brush:", GUILayout.Width(45f));
            DrawBrushButton(Direction.Up, "▲");
            DrawBrushButton(Direction.Down, "▼");
            DrawBrushButton(Direction.Left, "◀");
            DrawBrushButton(Direction.Right, "▶");

            GUILayout.Space(8f);
            Color prevErase = GUI.backgroundColor;
            GUI.backgroundColor = _erase ? EraseColor : prevErase;
            bool eraseOn = GUILayout.Toggle(_erase, "Erase", "Button", GUILayout.Width(60f));
            GUI.backgroundColor = prevErase;
            _erase = eraseOn;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Left-click stamps the brush, right-click erases.  Keys: W/A/S/D direction, E erase, R randomize, C clear.",
                EditorStyles.miniLabel);
        }

        private void DrawBrushButton(Direction dir, string glyph)
        {
            bool active = !_erase && _brush == dir;

            Color prev = GUI.backgroundColor;
            Color c = DirectionColor(dir);
            // Full color when selected, dimmed toward the default otherwise.
            GUI.backgroundColor = active ? c : Color.Lerp(prev, c, 0.45f);

            bool clicked = GUILayout.Toggle(active, glyph, "Button", GUILayout.Width(34f));
            GUI.backgroundColor = prev;

            if (clicked && (!active || _erase))
            {
                _brush = dir;
                _erase = false;
            }
        }

        private void DrawRandomizer()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Head Count", GUILayout.Width(75f));
            _headCount = Mathf.Max(0, EditorGUILayout.IntField(_headCount, GUILayout.Width(50f)));
            GUILayout.Space(8f);
            if (ColorButton("Randomize", RandomizeColor, GUILayout.Width(100f)))
                Randomize();
            if (ColorButton("Clear", ClearColor, GUILayout.Width(60f)))
            {
                _heads.Clear();
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Randomize()
        {
            _heads.Clear();
            _heads.AddRange(_boardLogic.GenerateRandomLevel(_width, _height, _headCount));
            MarkDirty();
        }

        // Keyboard shortcuts: W/A/S/D pick the brush direction, E toggles erase,
        // R randomizes (with a confirmation so an accidental press can be undone).
        // Ignored while typing in a text/number field.
        private void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
                return;

            switch (e.keyCode)
            {
                case KeyCode.W: SetBrush(Direction.Up); break;
                case KeyCode.S: SetBrush(Direction.Down); break;
                case KeyCode.A: SetBrush(Direction.Left); break;
                case KeyCode.D: SetBrush(Direction.Right); break;
                case KeyCode.E: _erase = !_erase; break;
                case KeyCode.R:

                    if (EditorUtility.DisplayDialog("Randomize Level",
                            "Replace the current layout with a new random one?",
                            "Randomize", "Cancel"))
                        Randomize();
                    break;

                case KeyCode.C:
                    if (EditorUtility.DisplayDialog("Clear Level",
                          "Are you sure you want to clear the level?",
                          "Clear", "Cancel"))
                    {
                        _heads.Clear();
                        MarkDirty();
                    }
                    break;
                default: return;
            }

            e.Use();
            Repaint();
        }

        private void SetBrush(Direction dir)
        {
            _brush = dir;
            _erase = false;
        }

        // ---- Colors --------------------------------------------------------

        private static readonly Color EraseColor = new Color(0.85f, 0.30f, 0.30f);
        private static readonly Color SaveColor = new Color(0.30f, 0.72f, 0.38f);
        private static readonly Color RandomizeColor = new Color(0.30f, 0.55f, 0.90f);
        private static readonly Color ClearColor = new Color(0.80f, 0.45f, 0.25f);
        private static readonly Color CellColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color ViolationColor = new Color(0.60f, 0.20f, 0.20f);

        private static Color DirectionColor(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return new Color(0.35f, 0.78f, 0.40f);   // green
                case Direction.Down: return new Color(0.95f, 0.55f, 0.20f); // orange
                case Direction.Left: return new Color(0.30f, 0.55f, 0.95f); // blue
                case Direction.Right: return new Color(0.92f, 0.80f, 0.20f); // yellow
                default: return Color.gray;
            }
        }

        private static bool ColorButton(string label, Color color, params GUILayoutOption[] options)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(label, options);
            GUI.backgroundColor = prev;
            return clicked;
        }

        private void RevalidateForDisplay()
        {
            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            {
                width = _width,
                height = _height,
                heads = _heads
            });
            _violationCoords = new HashSet<Vector2Int>(result.violations.Select(v => v.coordinates));
        }

        private void DrawGrid()
        {
            var area = GUILayoutUtility.GetRect(_width * CellSize, _height * CellSize,
                GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            var glyphStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };

            var borderColor = new Color(0.45f, 0.45f, 0.45f);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var coord = new Vector2Int(x, y);
                    // Invert y so (0,0) is bottom-left.
                    var cell = new Rect(
                        area.x + x * CellSize,
                        area.y + (_height - 1 - y) * CellSize,
                        CellSize, CellSize);

                    // Border = full cell painted in the border color, then an inset fill.
                    EditorGUI.DrawRect(cell, borderColor);
                    var inner = new Rect(cell.x + 1f, cell.y + 1f, cell.width - 2f, cell.height - 2f);

                    bool violation = _violationCoords.Contains(coord);
                    bool occupied = TryGetHead(coord, out var head) && head.direction != Direction.None;

                    Color fill = CellColor;
                    if (violation)
                        fill = ViolationColor;
                    else if (occupied)
                        fill = Color.Lerp(CellColor, DirectionColor(head.direction), 0.25f);

                    EditorGUI.DrawRect(inner, fill);

                    if (occupied)
                    {
                        glyphStyle.normal.textColor = violation ? Color.white : DirectionColor(head.direction);
                        GUI.Label(inner, Glyph(head.direction), glyphStyle);
                    }

                    HandleCellMouse(cell, coord);
                }
            }
        }

        private void HandleCellMouse(Rect rect, Vector2Int coord)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition))
                return;

            if (e.button == 1 || _erase)
                RemoveHead(coord);
            else if (e.button == 0)
                StampHead(coord, _brush);
            else
                return;

            MarkDirty();
            e.Use();
        }

        private void DrawSaveRow()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (ColorButton("Save", SaveColor, GUILayout.Width(120f), GUILayout.Height(26f)))
                Save();
            EditorGUILayout.EndHorizontal();
        }

        // ---- Working-buffer edits ------------------------------------------

        private bool TryGetHead(Vector2Int coord, out HeadData head)
        {
            for (int i = 0; i < _heads.Count; i++)
            {
                if (_heads[i].coordinates == coord)
                {
                    head = _heads[i];
                    return true;
                }
            }

            head = default;
            return false;
        }

        private void StampHead(Vector2Int coord, Direction dir)
        {
            for (int i = 0; i < _heads.Count; i++)
            {
                if (_heads[i].coordinates == coord)
                {
                    _heads[i] = new HeadData { coordinates = coord, direction = dir };
                    return;
                }
            }

            _heads.Add(new HeadData { coordinates = coord, direction = dir });
        }

        private void RemoveHead(Vector2Int coord) =>
            _heads.RemoveAll(head => head.coordinates == coord);

        private void MarkDirty()
        {
            _dirty = true;
            Repaint();
        }

        // ---- Save ----------------------------------------------------------

        private void Save()
        {
            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            {
                width = _width,
                height = _height,
                heads = _heads
            });

            if (!result.isValid)
            {
                string msg = "Cannot Save:\n" + string.Join("\n",
                    result.violations.Select(v => $"{v.coordinates} {v.reason}"));
                EditorUtility.DisplayDialog("Cannot Save Level", msg, "OK");
                return;
            }

            Undo.RecordObject(_level, "Save Level");
            _level.width = _width;
            _level.height = _height;
            _level.heads = new List<HeadData>(_heads);
            EditorUtility.SetDirty(_level);
            AssetDatabase.SaveAssets();
            _dirty = false;
        }

        private static string Glyph(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return "▲";
                case Direction.Down: return "▼";
                case Direction.Left: return "◀";
                case Direction.Right: return "▶";
                default: return string.Empty;
            }
        }
    }
}
