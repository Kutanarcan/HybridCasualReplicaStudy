using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BallSortDesigner;

public class MagicSortEditorWindow : EditorWindow
{
    private const string DefaultFolder = "Assets/MagicSort/Levels";

    // ── Logic ──
    private MagicSortLevelEditorLogic _logic;

    // ── Level assets (one asset = one level, Arrows style) ──
    private readonly List<MagicSortLevel> _levels = new List<MagicSortLevel>();
    private string[] _levelNames = new string[0];
    private int _selectedIndex;
    private MagicSortLevel _level;
    private bool _dirty;

    // ── UI State ──
    private int _colorCount = 3;
    private int _emptyBars;
    private int _depthLevel = 1;
    private int _fragLevel = 1;
    private int _selectedBar = -1;
    private SolveResult? _solveResult;
    private bool _isProcessing;
    private Vector2 _scroll;

    // ── Colors ──
    private static readonly Color[] BarColors =
    {
        new Color(0.86f, 0.15f, 0.15f), // Red
        new Color(0.15f, 0.39f, 0.92f), // Blue
        new Color(0.92f, 0.70f, 0.03f), // Yellow
        new Color(0.09f, 0.64f, 0.26f), // Green
        new Color(0.98f, 0.45f, 0.09f), // Orange
        new Color(0.58f, 0.20f, 0.92f), // Purple
        new Color(0.02f, 0.71f, 0.83f), // Cyan
        new Color(0.93f, 0.27f, 0.60f), // Pink
        new Color(0.47f, 0.44f, 0.40f), // Brown
        new Color(0.94f, 0.96f, 0.98f), // White
    };

    private static readonly string[] ColorLabels =
        { "R", "B", "Y", "G", "O", "P", "C", "Pi", "Br", "W" };

    private static readonly string[] ColorNames =
        { "Red", "Blue", "Yellow", "Green", "Orange", "Purple", "Cyan", "Pink", "Brown", "White" };

    private static readonly string[] LevelLabels = { "Easy", "Medium", "Hard" };

    // ── Constants ──
    private const int SlotW = 44;
    private const int SlotH = 38;
    private const int SlotGap = 3;
    private const int BarPadding = 4;
    private const int BarSpacing = 16;
    private const int BarHeight = 4;

    [MenuItem("Tools/Ball Sort Designer")]
    public static void ShowWindow()
    {
        var w = GetWindow<MagicSortEditorWindow>("Ball Sort Designer");
        w.minSize = new Vector2(500, 520);
    }

    private void OnEnable()
    {
        RefreshLevels();
    }

    // Reloads the working board with a fresh solved level for the current config.
    private void RebuildLogic()
    {
        var data = MagicSortGenerator.CreateSolvedLevel(_colorCount, BarHeight, _emptyBars);
        if (_logic == null)
            _logic = new MagicSortLevelEditorLogic(data);
        else
            _logic.LoadLevel(data);
    }

    private void OnGUI()
    {
        if (_logic == null)
            RebuildLogic();

        DrawToolbar();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawConfig();
        EditorGUILayout.Space(6);
        DrawControls();
        EditorGUILayout.Space(4);
        DrawSaveRow();
        EditorGUILayout.Space(4);
        DrawSolveResult();
        EditorGUILayout.Space(4);
        DrawMetrics();
        EditorGUILayout.Space(8);
        DrawBars();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField(
            "Designer free-move: move balls however you like. \"Can it be solved?\" / Save check against the actual game rules.",
            EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();
    }

    // ────────────────────────────────────────
    //  Level assets (discover / select / create / save)
    // ────────────────────────────────────────

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Level:", EditorStyles.miniLabel, GUILayout.Width(38));
        int picked = EditorGUILayout.Popup(_selectedIndex, _levelNames,
            EditorStyles.toolbarPopup, GUILayout.Width(180));
        if (picked != _selectedIndex)
            SelectLevel(picked);

        if (GUILayout.Button("New Level", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Level", "MagicSortLevel", "asset",
                "Choose a name and location for the new level.", DefaultFolder);
            if (!string.IsNullOrEmpty(path))
            {
                CreateLevel(path);
                GUIUtility.ExitGUI();
            }
        }

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            RefreshLevels();

        GUILayout.FlexibleSpace();

        if (_dirty)
            GUILayout.Label("● Unsaved", EditorStyles.miniLabel);

        using (new EditorGUI.DisabledScope(_level == null))
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(46)))
                EditorGUIUtility.PingObject(_level);

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshLevels()
    {
        string previousPath = _level != null ? AssetDatabase.GetAssetPath(_level) : null;

        _levels.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:MagicSortLevel"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var lvl = AssetDatabase.LoadAssetAtPath<MagicSortLevel>(path);
            if (lvl != null)
                _levels.Add(lvl);
        }

        _levels.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        _levelNames = _levels.Select(l => l.name).ToArray();

        if (_levels.Count == 0)
        {
            _level = null;
            _selectedIndex = 0;
            if (_logic == null)
                RebuildLogic();
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

    private void LoadWorkingCopy(MagicSortLevel level)
    {
        var bars = level.ToBars();
        if (_logic == null)
            _logic = new MagicSortLevelEditorLogic(bars);
        else
            _logic.LoadLevel(bars);

        // Restore the config controls from the loaded layout.
        _colorCount = Mathf.Max(1, CountColors(bars));
        _emptyBars = Mathf.Max(0, bars.Count - _colorCount);
        _selectedBar = -1;
        _solveResult = null;
        _dirty = false;
    }

    private static int CountColors(IReadOnlyList<Bar> bars)
    {
        var seen = new HashSet<int>();
        foreach (var bar in bars)
            for (int i = 0; i < bar.Count; i++)
                seen.Add(bar[i]);
        return seen.Count;
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

        var level = CreateInstance<MagicSortLevel>();
        level.SetFromBars(_logic.Snapshot());
        AssetDatabase.CreateAsset(level, assetPath);
        AssetDatabase.SaveAssets();

        RefreshLevels();
        int idx = _levels.IndexOf(level);
        if (idx >= 0)
            SelectLevel(idx, force: true);
    }

    private void DrawSaveRow()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        using (new EditorGUI.DisabledScope(_level == null))
            if (GUILayout.Button("Save Level", GUILayout.Width(120), GUILayout.Height(24)))
                Save();
        EditorGUILayout.EndHorizontal();
    }

    private void Save()
    {
        if (_level == null)
            return;

        var bars = _logic.Snapshot();
        var result = MagicSortSolver.Solve(bars);
        _solveResult = result;

        if (result.Status == SolveStatus.Unsolvable)
        {
            EditorUtility.DisplayDialog("Cannot Save Level",
                "This layout is unsolvable under the game rules. Fix it before saving.", "OK");
            return;
        }

        if (result.Status == SolveStatus.Timeout &&
            !EditorUtility.DisplayDialog("Solvability Unknown",
                "The solver hit its state limit and could not confirm this level is solvable. Save anyway?",
                "Save", "Cancel"))
            return;

        Undo.RecordObject(_level, "Save Magic Sort Level");
        _level.SetFromBars(bars);
        EditorUtility.SetDirty(_level);
        AssetDatabase.SaveAssets();
        _dirty = false;
    }

    private void MarkDirty()
    {
        _dirty = true;
        Repaint();
    }

    // ────────────────────────────────────────
    //  Config
    // ────────────────────────────────────────

    private void DrawConfig()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        // Color count
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Color", GUILayout.Width(100));
        for (int n = 3; n <= 10; n++)
        {
            bool active = n == _colorCount;
            GUI.backgroundColor = active ? new Color(0.3f, 0.5f, 0.9f) : Color.white;
            if (GUILayout.Button(n.ToString(), GUILayout.Width(28), GUILayout.Height(22)))
            {
                _colorCount = n;
                Reconfigure();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Empty bars
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Extra Empty Bar", GUILayout.Width(100));
        for (int e = 0; e <= 3; e++)
        {
            bool active = e == _emptyBars;
            GUI.backgroundColor = active ? new Color(0.3f, 0.5f, 0.9f) : Color.white;
            if (GUILayout.Button(e.ToString(), GUILayout.Width(28), GUILayout.Height(22)))
            {
                _emptyBars = e;
                Reconfigure();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Depth
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Depth", GUILayout.Width(100));
        for (int i = 0; i < 3; i++)
        {
            bool active = i == _depthLevel;
            GUI.backgroundColor = active ? new Color(0.9f, 0.5f, 0.1f) : Color.white;
            if (GUILayout.Button(LevelLabels[i], GUILayout.Width(60), GUILayout.Height(22)))
                _depthLevel = i;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Fragmentation
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Fragmentation", GUILayout.Width(100));
        for (int i = 0; i < 3; i++)
        {
            bool active = i == _fragLevel;
            GUI.backgroundColor = active ? new Color(0.6f, 0.3f, 0.9f) : Color.white;
            if (GUILayout.Button(LevelLabels[i], GUILayout.Width(60), GUILayout.Height(22)))
                _fragLevel = i;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    // ────────────────────────────────────────
    //  Controls
    // ────────────────────────────────────────

    private void DrawControls()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"Move Count: {_logic.MoveCount}", GUILayout.Width(80));

        GUI.enabled = _logic.CanUndo;
        if (GUILayout.Button("Undo Move", GUILayout.Height(24)))
        {
            _logic.Undo();
            _solveResult = null;
            _selectedBar = -1;
            MarkDirty();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Reset", GUILayout.Height(24)))
        {
            RebuildLogic();
            _solveResult = null;
            _selectedBar = -1;
            MarkDirty();
        }

        GUI.enabled = !_isProcessing && _emptyBars > 0;
        if (GUILayout.Button(_isProcessing ? "Searching..." : "Scramble", GUILayout.Height(24)))
        {
            _selectedBar = -1;
            var data = MagicSortGenerator.Generate(
                _colorCount, BarHeight, _emptyBars, _depthLevel, _fragLevel, out var result);
            _logic.LoadLevel(data);
            _solveResult = result;
            MarkDirty();
        }
        GUI.enabled = !_isProcessing && !_logic.IsPuzzleSolved();
        if (GUILayout.Button("Can it be solved?", GUILayout.Height(24)))
        {
            _selectedBar = -1;
            _solveResult = _logic.CheckCurrentState();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    // ────────────────────────────────────────
    //  Solve Result Banner
    // ────────────────────────────────────────

    private void DrawSolveResult()
    {
        if (_logic.IsPuzzleSolved() && _logic.MoveCount > 0)
        {
            DrawBanner($"Solved! ({_logic.MoveCount} Move)", new Color(0.1f, 0.5f, 0.2f));
        }

        if (!_solveResult.HasValue) return;
        var r = _solveResult.Value;

        switch (r.Status)
        {
            case SolveStatus.Solved:
                DrawBanner("Already Solved!", new Color(0.1f, 0.5f, 0.2f));
                break;
            case SolveStatus.Solvable:
                string extra = r.Attempts > 0 ? $"  ({r.Attempts} attempt)" : "";
                DrawBanner($"Solvable — min {r.MinMoves} move{extra}", new Color(0.1f, 0.5f, 0.2f));
                break;
            case SolveStatus.Unsolvable:
                DrawBanner("Unsolvable", new Color(0.7f, 0.15f, 0.15f));
                break;
            case SolveStatus.Timeout:
                DrawBanner($"Ambiguous — {r.StatesExplored / 1000}K case, reached limit",
                    new Color(0.7f, 0.5f, 0.1f));
                break;
        }
    }

    private void DrawBanner(string text, Color bgColor)
    {
        var rect = EditorGUILayout.GetControlRect(false, 24);
        EditorGUI.DrawRect(rect, bgColor);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontSize = 12
        };
        GUI.Label(rect, text, style);
    }

    // ────────────────────────────────────────
    //  Metrics
    // ────────────────────────────────────────

    private void DrawMetrics()
    {
        var m = MagicSortGenerator.CalculateMetrics(_logic.Snapshot());
        DrawMetricBar("Depth", m.Depth, new Color(0.98f, 0.45f, 0.09f));
        DrawMetricBar("Fragmentation", m.Fragmentation, new Color(0.66f, 0.33f, 0.97f));
    }

    private void DrawMetricBar(string label, float value, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(100));

        var rect = EditorGUILayout.GetControlRect(false, 12, GUILayout.Width(140));
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.2f));
        var fill = new Rect(rect.x, rect.y, rect.width * value, rect.height);
        EditorGUI.DrawRect(fill, color);

        EditorGUILayout.LabelField($"{Mathf.RoundToInt(value * 100)}%", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
    }

    // ────────────────────────────────────────
    //  Bars Drawing
    // ────────────────────────────────────────

    private void DrawBars()
    {
        int total = _logic.GetBarCount();
        int barsPerRow = Mathf.Min(total, 6);
        int rows = Mathf.CeilToInt((float)total / barsPerRow);
        int barW = SlotW + BarPadding * 2;
        int barTotalH = BarHeight * (SlotH + SlotGap) + BarPadding;

        for (int row = 0; row < rows; row++)
        {
            int rowStart = row * barsPerRow;
            int rowCount = Mathf.Min(barsPerRow, total - rowStart);
            float totalW = rowCount * barW + (rowCount - 1) * BarSpacing;

            var rowRect = EditorGUILayout.GetControlRect(false, barTotalH + 22);
            float startX = rowRect.x + (rowRect.width - totalW) * 0.5f;

            for (int col = 0; col < rowCount; col++)
            {
                int bi = rowStart + col;
                float x = startX + col * (barW + BarSpacing);
                float y = rowRect.y;

                DrawSingleBar(bi, x, y, barW, barTotalH);
            }
        }
    }

    private void DrawSingleBar(int barIndex, float x, float y, int barW, int barTotalH)
    {
        bool isSelected = _selectedBar == barIndex;
        bool isEmpty = barIndex >= _colorCount;

        // Bar background
        var barRect = new Rect(x, y, barW, barTotalH);
        EditorGUI.DrawRect(barRect, isSelected
            ? new Color(0.12f, 0.23f, 0.38f)
            : new Color(0.12f, 0.14f, 0.18f));

        // Border
        DrawRectBorder(barRect, isSelected
            ? new Color(0.38f, 0.65f, 0.98f)
            : new Color(0.2f, 0.22f, 0.27f));

        // Slots (bottom to top)
        for (int si = 0; si < BarHeight; si++)
        {
            float slotX = x + BarPadding;
            float slotY = y + barTotalH - BarPadding - (si + 1) * (SlotH + SlotGap) + SlotGap;
            var slotRect = new Rect(slotX, slotY, SlotW, SlotH);

            if (si < _logic.GetBarSize(barIndex))
            {
                int colorIdx = _logic.GetColor(barIndex, si);
                bool isTop = si == _logic.GetBarSize(barIndex) - 1;
                Color c = colorIdx >= 0 && colorIdx < BarColors.Length
                    ? BarColors[colorIdx] : Color.gray;

                if (!isTop || isSelected) c *= 0.8f;
                c.a = 1f;

                EditorGUI.DrawRect(slotRect, c);

                var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = colorIdx == 9
                        ? new Color(0.1f, 0.1f, 0.15f) : Color.white }
                };
                string label = colorIdx >= 0 && colorIdx < ColorLabels.Length
                    ? ColorLabels[colorIdx] : "?";
                GUI.Label(slotRect, label, labelStyle);

                // Glow on selected top
                if (isTop && isSelected)
                    DrawRectBorder(slotRect, new Color(0.38f, 0.65f, 0.98f, 0.8f));
            }
            else
            {
                // Empty slot dashed look
                DrawRectBorder(slotRect, new Color(0.2f, 0.22f, 0.27f, 0.4f));
            }
        }

        // Click detection
        if (Event.current.type == EventType.MouseDown
            && barRect.Contains(Event.current.mousePosition))
        {
            HandleBarClick(barIndex);
            Event.current.Use();
        }

        // Label
        var labelRect = new Rect(x, y + barTotalH + 2, barW, 16);
        var nameStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 9,
            normal = { textColor = isSelected
                ? new Color(0.38f, 0.65f, 0.98f)
                : new Color(0.4f, 0.42f, 0.47f) }
        };
        string barLabel = isEmpty
            ? $"Empty {barIndex - _colorCount + 1}"
            : ColorNames[barIndex];
        GUI.Label(labelRect, barLabel, nameStyle);
    }

    // ────────────────────────────────────────
    //  Interaction
    // ────────────────────────────────────────

    private void HandleBarClick(int barIndex)
    {
        if (_selectedBar == -1)
        {
            if (_logic.IsBarEmpty(barIndex)) return;
            _selectedBar = barIndex;
        }
        else
        {
            if (barIndex == _selectedBar)
            {
                _selectedBar = -1;
            }
            else if (_logic.TryMove(_selectedBar, barIndex))
            {
                _selectedBar = -1;
                _solveResult = null;
                MarkDirty();
            }
            else
            {
                _selectedBar = -1;
            }
        }
        Repaint();
    }

    // ────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────

    private void Reconfigure()
    {
        RebuildLogic();
        _selectedBar = -1;
        _solveResult = null;
        MarkDirty();
    }

    private static void DrawRectBorder(Rect rect, Color color, float thickness = 1f)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }
}
