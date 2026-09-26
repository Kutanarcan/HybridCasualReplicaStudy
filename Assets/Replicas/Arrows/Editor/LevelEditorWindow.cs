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
        private enum BrushMode { Direction, Line, Erase }
        private BrushMode _mode = BrushMode.Direction;
        private Direction _brush = Direction.Up;
        private Vector2Int? _selectedHead; // active head while painting lines
        private int _headCount = 5;

        private readonly BoardLogic _boardLogic = new();
        private HashSet<Vector2Int> _violationCoords = new();

        [MenuItem("Tools/Level Design/Arrows Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Arrows Replica Level Editor");
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
            _heads.AddRange(CloneHeads(level.heads));
            _selectedHead = null;
            _dirty = false;
        }

        // Deep-copies the head list so the working buffer never shares line lists with the asset.
        private static IEnumerable<HeadData> CloneHeads(IEnumerable<HeadData> source)
        {
            foreach (var head in source)
                yield return new HeadData
                {
                    coordinates = head.coordinates,
                    direction = head.direction,
                    line = head.line != null ? new List<LineCell>(head.line) : new List<LineCell>()
                };
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
            DrawSaveRow();

            EditorGUILayout.Space();
            RevalidateForDisplay();
            DrawGrid();

         
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

            // Trim line cells that fell outside the new bounds (from the first offender onward).
            for (int i = 0; i < _heads.Count; i++)
            {
                var head = _heads[i];
                if (head.line == null)
                    continue;

                int cut = head.line.FindIndex(c => c.coordinates.x >= w || c.coordinates.y >= h);
                if (cut >= 0)
                {
                    head.line.RemoveRange(cut, head.line.Count - cut);
                    _heads[i] = head;
                }
            }

            if (_selectedHead.HasValue &&
                (_selectedHead.Value.x >= w || _selectedHead.Value.y >= h))
                _selectedHead = null;

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
            DrawModeToggle(BrushMode.Line, "Line", EraseColor);
            GUILayout.Space(4f);
            DrawModeToggle(BrushMode.Erase, "Erase", EraseColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Heads: left-click stamps, right-click erases.  Line: click a head to select it, then click adjacent cells to extend its line; right-click trims.",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "Keys: W/A/S/D direction, L line, E erase, R randomize, C clear.",
                EditorStyles.miniLabel);
        }

        private void DrawModeToggle(BrushMode mode, string label, Color color)
        {
            bool active = _mode == mode;
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = active ? color : prev;
            bool on = GUILayout.Toggle(active, label, "Button", GUILayout.Width(60f));
            GUI.backgroundColor = prev;

            if (on && !active)
                SetMode(mode);
        }

        private void DrawBrushButton(Direction dir, string glyph)
        {
            bool active = _mode == BrushMode.Direction && _brush == dir;

            Color prev = GUI.backgroundColor;
            Color c = DirectionColor(dir);
            // Full color when selected, dimmed toward the default otherwise.
            GUI.backgroundColor = active ? c : Color.Lerp(prev, c, 0.45f);

            bool clicked = GUILayout.Toggle(active, glyph, "Button", GUILayout.Width(34f));
            GUI.backgroundColor = prev;

            if (clicked && !active)
            {
                _brush = dir;
                SetMode(BrushMode.Direction);
            }
        }

        private void SetMode(BrushMode mode)
        {
            _mode = mode;
            if (mode != BrushMode.Line)
                _selectedHead = null;
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
                _selectedHead = null;
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Randomize()
        {
            _heads.Clear();
            _heads.AddRange(_boardLogic.GenerateRandomLevel(_width, _height, _headCount));
            _selectedHead = null;
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
                case KeyCode.L: SetMode(BrushMode.Line); break;
                case KeyCode.E: SetMode(_mode == BrushMode.Erase ? BrushMode.Direction : BrushMode.Erase); break;
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
                        _selectedHead = null;
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
            SetMode(BrushMode.Direction);
        }

        // ---- Colors --------------------------------------------------------

        private static readonly Color EraseColor = new Color(0.85f, 0.30f, 0.30f);
        private static readonly Color SaveColor = new Color(0.30f, 0.72f, 0.38f);
        private static readonly Color RandomizeColor = new Color(0.30f, 0.55f, 0.90f);
        private static readonly Color ClearColor = new Color(0.80f, 0.45f, 0.25f);
        private static readonly Color CellColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color ViolationColor = new Color(0.60f, 0.20f, 0.20f);
        private static readonly Color SelectedColor = new Color(0.95f, 0.95f, 0.40f);

        // 50 distinct colors, no red (reserved for violations).
        // Golden-ratio hue step: each index lands as far as possible from all previous ones.
        private static readonly Color[] HeadColorPool = BuildColorPool();

        private static Color[] BuildColorPool()
        {
            var pool = new Color[50];
            float h = 0f;
            for (int i = 0; i < 50; i++)
            {
                h = (h + 0.618034f) % 1f; // golden-ratio conjugate
                // Remap [0,1] → [0.08, 0.93] to keep red (~0° and ~360°) out of range.
                float safeHue = 0.08f + h * 0.85f;
                float sat = i % 2 == 0 ? 0.80f : 0.65f;
                float val = i % 3 == 0 ? 0.95f : i % 3 == 1 ? 0.80f : 1.00f;
                pool[i] = Color.HSVToRGB(safeHue, sat, val);
            }
            return pool;
        }

        private static Color PoolColor(int headIndex) =>
            HeadColorPool[headIndex % HeadColorPool.Length];

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

            var numberStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 9
            };
            numberStyle.normal.textColor = Color.white;

            var lineNumberStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            lineNumberStyle.normal.textColor = Color.white;

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

                    int lineOwner = 0;
                    int lineCellIdx = -1;

                    bool violation = _violationCoords.Contains(coord);
                    bool isHead = TryGetHeadIndex(coord, out int headIdx);
                    bool isLine = !isHead && TryGetLineOwner(coord, out lineOwner, out lineCellIdx);

                    Color fill = CellColor;
                    if (violation)
                        fill = ViolationColor;
                    else if (isHead)
                        fill = Color.Lerp(CellColor, PoolColor(headIdx), 0.55f);
                    else if (isLine)
                        fill = Color.Lerp(CellColor, PoolColor(lineOwner), 0.35f);

                    EditorGUI.DrawRect(inner, fill);

                    if (isHead)
                    {
                        var head = _heads[headIdx];
                        if (head.direction != Direction.None)
                        {
                            glyphStyle.normal.textColor = violation ? Color.white : PoolColor(headIdx);
                            GUI.Label(inner, Glyph(head.direction), glyphStyle);
                        }

                        // Index links a head to its line cells.
                        GUI.Label(new Rect(inner.x + 4f, inner.y + 3f, inner.width, 20f),
                            headIdx.ToString(), numberStyle);

                        if (_selectedHead == coord)
                            DrawOutline(inner, SelectedColor);
                    }
                    else if (isLine)
                    {
                        // Direction glyph shows flow direction so the designer can see the tail.
                        var lineCell = _heads[lineOwner].line[lineCellIdx];
                        if (lineCell.direction != Direction.None)
                        {
                            var c = PoolColor(lineOwner);
                            var dimStyle = new GUIStyle(glyphStyle);
                            dimStyle.normal.textColor = new Color(c.r, c.g, c.b, 0.60f);
                            dimStyle.fontSize = 16;
                            GUI.Label(inner, GlyphLine(lineCell.direction), dimStyle);
                        }
                        // Small owner index in corner links cell to its head.
                        GUI.Label(new Rect(inner.x + 4f, inner.y + 3f, inner.width, 20f),
                            lineOwner.ToString(), numberStyle);
                    }

                    HandleCellMouse(cell, coord);
                }
            }
        }

        // 1px inset outline drawn as four thin rects.
        private static void DrawOutline(Rect r, Color color)
        {
            const float t = 2f;
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), color);
        }

        private void HandleCellMouse(Rect rect, Vector2Int coord)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition))
                return;

            bool changed;

            if (e.button == 1)
                changed = _mode == BrushMode.Line ? TrimLineAt(coord) : RemoveAt(coord);
            else if (e.button == 0)
                changed = PaintAt(coord);
            else
                return;

            if (changed)
                MarkDirty();
            else
                Repaint(); // selection / no-op still needs a redraw

            e.Use();
        }

        // Left-click behaviour for the active brush mode. Returns true if the level changed.
        private bool PaintAt(Vector2Int coord)
        {
            switch (_mode)
            {
                case BrushMode.Direction: StampHead(coord, _brush); return true;
                case BrushMode.Erase:     return RemoveAt(coord);
                case BrushMode.Line:      return PaintLine(coord);
                default:                  return false;
            }
        }

        // Click a head to select it; click a free cell adjacent to the selected head's chain to
        // extend it (never into the head's own line of sight).
        private bool PaintLine(Vector2Int coord)
        {
            if (TryGetHeadIndex(coord, out _))
            {
                _selectedHead = coord;
                return false;
            }

            if (!_selectedHead.HasValue || !TryGetHeadIndex(_selectedHead.Value, out int selIdx))
                return false;

            var head = _heads[selIdx];
            var tail = head.line is { Count: > 0 } ? head.line[^1].coordinates : head.coordinates;

            if ((coord - tail).sqrMagnitude != 1 || !IsCellFree(coord) || InOwnLineOfSight(head, coord))
                return false;

            head.line ??= new List<LineCell>();
            head.line.Add(new LineCell { coordinates = coord, direction = (coord - tail).ToDirection() });
            _heads[selIdx] = head;
            return true;
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

        private bool TryGetHeadIndex(Vector2Int coord, out int index)
        {
            for (int i = 0; i < _heads.Count; i++)
            {
                if (_heads[i].coordinates == coord)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        // Finds the head that owns a line cell, returning the head index and the cell's slot in its line.
        private bool TryGetLineOwner(Vector2Int coord, out int headIndex, out int cellIndex)
        {
            for (int i = 0; i < _heads.Count; i++)
            {
                var line = _heads[i].line;
                if (line == null)
                    continue;

                int slot = line.FindIndex(c => c.coordinates == coord);
                if (slot >= 0)
                {
                    headIndex = i;
                    cellIndex = slot;
                    return true;
                }
            }

            headIndex = -1;
            cellIndex = -1;
            return false;
        }

        // In-bounds, not a head, not already part of any line.
        private bool IsCellFree(Vector2Int coord)
        {
            if (coord.x < 0 || coord.x >= _width || coord.y < 0 || coord.y >= _height)
                return false;

            return !TryGetHeadIndex(coord, out _) && !TryGetLineOwner(coord, out _, out _);
        }

        // True if coord lies on the ray from the head, in its direction, to the board edge.
        private bool InOwnLineOfSight(HeadData head, Vector2Int coord)
        {
            var step = head.direction.ToVector2Int();
            if (step == Vector2Int.zero)
                return false;

            var c = head.coordinates + step;
            while (c.x >= 0 && c.x < _width && c.y >= 0 && c.y < _height)
            {
                if (c == coord)
                    return true;
                c += step;
            }

            return false;
        }

        private void StampHead(Vector2Int coord, Direction dir)
        {
            if (TryGetHeadIndex(coord, out int i))
            {
                // Keep the existing line when only changing direction.
                var head = _heads[i];
                head.direction = dir;
                _heads[i] = head;
                return;
            }

            _heads.Add(new HeadData { coordinates = coord, direction = dir, line = new List<LineCell>() });
        }

        // Erase: removes a whole head (head + its line) or trims a line cell. Returns true if changed.
        private bool RemoveAt(Vector2Int coord)
        {
            if (TryGetHeadIndex(coord, out int headIdx))
            {
                if (_selectedHead == coord)
                    _selectedHead = null;
                _heads.RemoveAt(headIdx);
                return true;
            }

            return TrimLineAt(coord);
        }

        // Removes a line cell and everything past it on the chain (keeps contiguity).
        private bool TrimLineAt(Vector2Int coord)
        {
            if (!TryGetLineOwner(coord, out int headIdx, out int cellIdx))
                return false;

            var head = _heads[headIdx];
            head.line.RemoveRange(cellIdx, head.line.Count - cellIdx);
            _heads[headIdx] = head;
            return true;
        }

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
            _level.heads = new List<HeadData>(CloneHeads(_heads));
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

        private static string GlyphLine(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return "▲L";
                case Direction.Down: return "▼L";
                case Direction.Left: return "◀L";
                case Direction.Right: return "▶L";
                default: return string.Empty;
            }
        }
    }
}
