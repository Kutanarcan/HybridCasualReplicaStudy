using System.Collections.Generic;
using ReplicaProjects.Common.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Arrows.EditorTools
{
    /// <summary>
    /// Arrows level editor: edits an in-memory copy of a LevelData asset and writes it back only on a
    /// validated save. Board editing rules live in Core (LevelEditBuffer / LinePainter).
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        private readonly LevelEditBuffer _buffer = new();
        private readonly LevelBrushBar _brushBar = new();
        private readonly LevelGridView _gridView = new();
        private readonly BoardValidator _validator = new();
        private readonly HashSet<GridCoord> _violationCoords = new();

        private LevelAssetBrowser<LevelData> _browser;
        private LevelPaintTool _paintTool;
        private LevelBoardControls _boardControls;
        private bool _dirty;

        [MenuItem("Tools/Level Design/Arrows Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Arrows Replica Level Editor");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            _paintTool ??= new LevelPaintTool(_buffer, _brushBar);
            _boardControls ??= new LevelBoardControls(_buffer, _paintTool.Painter);
            _boardControls.Changed += MarkDirty;
            _gridView.CellClicked += OnCellClicked;

            _browser = new LevelAssetBrowser<LevelData>("Assets/Arrows/Levels", "Level",
                () => _dirty, LoadWorkingCopy, InitializeNewLevel);
            _browser.Refresh();
        }

        private void OnDisable()
        {
            _boardControls.Changed -= MarkDirty;
            _gridView.CellClicked -= OnCellClicked;
        }

        private void OnGUI()
        {
            _browser.DrawToolbar();

            if (_browser.Current == null)
            {
                EditorGUILayout.HelpBox("No LevelData asset selected. Create one to begin.", MessageType.Info);
                return;
            }

            HandleShortcuts();

            _boardControls.DrawDimensions();
            _brushBar.Draw();
            _boardControls.DrawRandomizer();

            EditorGUILayout.Space();
            DrawSaveRow();

            EditorGUILayout.Space();
            RevalidateForDisplay();
            _gridView.Draw(_buffer, _violationCoords, _paintTool.Painter.SelectedHead);
        }

        private void LoadWorkingCopy(LevelData level)
        {
            _buffer.Load(level.width, level.height, level.heads);
            _paintTool.Painter.Deselect();
            _dirty = false;
        }

        private void InitializeNewLevel(LevelData level)
        {
            level.width = _buffer.Width;
            level.height = _buffer.Height;
        }

        private void DrawSaveRow()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (EditorDraw.ColorButton("Save", LevelEditorStyle.SaveColor, GUILayout.Width(120f), GUILayout.Height(26f)))
                Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Save()
        {
            var result = Validate();
            if (!result.isValid)
            {
                EditorUtility.DisplayDialog("Cannot Save Level", FormatViolations(result), "OK");
                return;
            }

            var level = _browser.Current;
            Undo.RecordObject(level, "Save Level");
            level.width = _buffer.Width;
            level.height = _buffer.Height;
            level.heads = _buffer.CloneHeads();
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            _dirty = false;
        }

        private static string FormatViolations(BoardValidateResult result)
        {
            var lines = new string[result.violations.Count];
            for (int i = 0; i < lines.Length; i++)
                lines[i] = $"{result.violations[i].coordinates} {result.violations[i].reason}";
            return "Cannot Save:\n" + string.Join("\n", lines);
        }

        private BoardValidateResult Validate() => _validator.ValidateBoard(new BoardValidateInput
        {
            width = _buffer.Width,
            height = _buffer.Height,
            heads = _buffer.Heads
        });

        private void RevalidateForDisplay()
        {
            _violationCoords.Clear();
            foreach (var violation in Validate().violations)
                _violationCoords.Add(violation.coordinates);
        }

        // Ignored while typing in a text/number field.
        private void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
                return;

            if (!_brushBar.HandleKey(e.keyCode) && !_boardControls.HandleKey(e.keyCode))
                return;

            e.Use();
            Repaint();
        }

        private void OnCellClicked(GridCoord coord, int button)
        {
            if (_paintTool.Click(coord, button))
                MarkDirty();
            else
                Repaint(); // selection / no-op still needs a redraw
        }

        private void MarkDirty()
        {
            _dirty = true;
            Repaint();
        }
    }
}
