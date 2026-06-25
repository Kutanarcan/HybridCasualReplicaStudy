using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectManagement
{
    public class ProjectManagementWindow : EditorWindow
    {
        // Default location/name used only when no board exists yet.
        private const string DefaultBoardPath = "Assets/_ProjectManagement/ProjectTaskBoard.asset";

        // All ProjectTaskBoard assets found in the project; one is active at a time.
        private readonly List<ProjectTaskBoard> _boards = new List<ProjectTaskBoard>();
        private string[] _boardNames = new string[0];
        private int _selectedIndex;

        private ProjectTaskBoard _board; // the currently selected board
        private Vector2 _scroll;

        // Per-task fold state, keyed by task Id. Editor-only UI state (not saved to the asset).
        private readonly Dictionary<int, bool> _expanded = new Dictionary<int, bool>();

        private bool IsExpanded(int taskId)
        {
            return !_expanded.TryGetValue(taskId, out bool open) || open; // default: expanded
        }

        private void SetAllExpanded(bool open)
        {
            foreach (var task in _board.Tasks)
                _expanded[task.Id] = open;
        }

        [MenuItem("Window/Project Management/Task Board")]
        public static void Open()
        {
            var window = GetWindow<ProjectManagementWindow>();
            window.titleContent = new GUIContent("Task Board");
            window.minSize = new Vector2(660f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshBoards();
            if (_boards.Count == 0)
                CreateBoard(DefaultBoardPath); // bootstrap a first board
        }

        // Discover every ProjectTaskBoard asset in the project and keep the
        // current selection if it still exists.
        private void RefreshBoards()
        {
            string previousPath = _board != null ? AssetDatabase.GetAssetPath(_board) : null;

            _boards.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:ProjectTaskBoard"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var board = AssetDatabase.LoadAssetAtPath<ProjectTaskBoard>(path);
                if (board != null)
                    _boards.Add(board);
            }

            // Stable, readable order regardless of GUID/discovery order.
            _boards.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            _boardNames = new string[_boards.Count];
            for (int i = 0; i < _boards.Count; i++)
                _boardNames[i] = _boards[i].name;

            if (_boards.Count == 0)
            {
                _board = null;
                _selectedIndex = 0;
                return;
            }

            int restored = previousPath != null
                ? _boards.FindIndex(b => AssetDatabase.GetAssetPath(b) == previousPath)
                : -1;
            SelectBoard(restored >= 0 ? restored : 0);
        }

        private void SelectBoard(int index)
        {
            _selectedIndex = Mathf.Clamp(index, 0, _boards.Count - 1);
            _board = _boards[_selectedIndex];
            // Fold state is keyed by task Id, which only makes sense within one
            // board, so reset it when switching.
            _expanded.Clear();
            _scroll = Vector2.zero;
        }

        // Creates a new empty board at the given project-relative path, then
        // refreshes the list and selects it.
        private void CreateBoard(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var board = CreateInstance<ProjectTaskBoard>();
            AssetDatabase.CreateAsset(board, assetPath);
            AssetDatabase.SaveAssets();

            RefreshBoards();
            int idx = _boards.IndexOf(board);
            if (idx >= 0)
                SelectBoard(idx);
        }

        // Flush changes to disk so the .asset (and thus git) stays in sync.
        private void Persist()
        {
            EditorUtility.SetDirty(_board);
            AssetDatabase.SaveAssets();
        }

        private void OnGUI()
        {
            if (_board == null)
            {
                EditorGUILayout.HelpBox("No task board found.", MessageType.Warning);
                if (GUILayout.Button("Create board"))
                    CreateBoard(DefaultBoardPath);
                return;
            }

            DrawToolbar();
            DrawHeader();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _board.Tasks.Count; i++)
            {
                DrawTask(_board.Tasks[i], i);
                if (_board == null) // board was reloaded mid-draw
                    break;
            }
            EditorGUILayout.EndScrollView();

            if (_board != null && _board.Tasks.Count == 0)
                EditorGUILayout.HelpBox("No tasks yet. Use \"+ Add Task\" above.", MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Board selector: choose which ProjectTaskBoard asset is shown.
            GUILayout.Label("Board:", EditorStyles.miniLabel, GUILayout.Width(40f));
            int picked = EditorGUILayout.Popup(_selectedIndex, _boardNames,
                EditorStyles.toolbarPopup, GUILayout.Width(160f));
            if (picked != _selectedIndex)
                SelectBoard(picked);
            if (GUILayout.Button("New Board", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Task Board", "TaskBoard", "asset",
                    "Choose a name and location for the new task board.",
                    "Assets/_ProjectManagement");
                if (!string.IsNullOrEmpty(path))
                {
                    CreateBoard(path);
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                RefreshBoards();
            EditorGUILayout.Space();

            if (GUILayout.Button("+ Add Task", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                Undo.RecordObject(_board, "Add Task");
                _board.Tasks.Add(new Task { Id = _board.GetNextTaskId() });
                Persist();
            }
            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                SetAllExpanded(true);
            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                SetAllExpanded(false);
            GUILayout.FlexibleSpace();
            DrawLegend();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping Asset", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                EditorGUIUtility.PingObject(_board);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegend()
        {
            DrawSwatch(CardColors.For(CardType.Bug), "Bug");
            DrawSwatch(CardColors.For(CardType.Dev), "Dev");
            DrawSwatch(CardColors.For(CardType.Analyze), "Analyze");
        }

        private void DrawSwatch(Color color, string label)
        {
            var rect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
            rect.y += 4f;
            EditorGUI.DrawRect(rect, color);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(50f));
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(64f));
            GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(74f));
            GUILayout.Label("Description", EditorStyles.boldLabel);
            GUILayout.Label("Estimation", EditorStyles.boldLabel, GUILayout.Width(80f));
            GUILayout.Label("Progress", EditorStyles.boldLabel, GUILayout.Width(100f));
            GUILayout.Label("", GUILayout.Width(124f)); // action buttons column
            EditorGUILayout.EndHorizontal();
        }

        // Draws a colored vertical accent bar on the left edge of the last reserved rect row.
        private void DrawColorBar(Rect rowRect, Color color, float width = 4f)
        {
            var bar = new Rect(rowRect.x, rowRect.y, width, rowRect.height);
            EditorGUI.DrawRect(bar, color);
        }

        private void DrawTask(Task task, int index)
        {
            // Tint the whole card faintly by its type so the board reads at a glance.
            Color tint = CardColors.For(task.Type);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.Lerp(prevBg, tint, 0.20f);
            var card = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            DrawColorBar(new Rect(card.x, card.y, card.width, card.height), tint);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4f);

            // Foldout arrow to collapse/expand subtasks (disabled when there are none).
            bool expanded = IsExpanded(task.Id);
            using (new EditorGUI.DisabledScope(task.Subtasks.Count == 0))
            {
                bool toggled = GUILayout.Toggle(expanded, expanded ? "▼" : "▶",
                    EditorStyles.label, GUILayout.Width(16f));
                if (toggled != expanded)
                {
                    _expanded[task.Id] = toggled;
                    expanded = toggled;
                }
            }

            DrawIdBadge($"T-{task.Id}", tint);

            EditorGUI.BeginChangeCheck();
            var type = (CardType)EditorGUILayout.EnumPopup(task.Type, GUILayout.Width(74f));
            string desc = EditorGUILayout.TextField(task.Description);
            var est = (TaskEstimation)EditorGUILayout.EnumPopup(task.Estimation, GUILayout.Width(80f));
            DrawProgressPopup(ref task.Progress, out var prog, out bool progChanged);
            if (EditorGUI.EndChangeCheck() || progChanged)
            {
                Undo.RecordObject(_board, "Edit Task");
                task.Type = type;
                task.Description = desc;
                task.Estimation = est;
                task.Progress = prog;
                Persist();
            }

            // Reorder up/down; ID stays with the task.
            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("▲", GUILayout.Width(22f)))
                    MoveTask(index, index - 1);
            using (new EditorGUI.DisabledScope(index == _board.Tasks.Count - 1))
                if (GUILayout.Button("▼", GUILayout.Width(22f)))
                    MoveTask(index, index + 1);

            if (GUILayout.Button("+ST", GUILayout.Width(34f)))
            {
                Undo.RecordObject(_board, "Add Subtask");
                task.Subtasks.Add(new Subtask { Id = ProjectTaskBoard.GetNextSubtaskId(task), Type = task.Type });
                _expanded[task.Id] = true; // reveal the new subtask if the task was collapsed
                Persist();
            }
            if (ColoredButton("X", new Color(0.85f, 0.25f, 0.25f), 22f))
            {
                if (EditorUtility.DisplayDialog("Delete task",
                        $"Delete T-{task.Id}? Its ID will become reusable.", "Delete", "Cancel"))
                {
                    Undo.RecordObject(_board, "Delete Task");
                    _board.Tasks.RemoveAt(index);
                    Persist();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (expanded)
            {
                for (int s = 0; s < task.Subtasks.Count; s++)
                    DrawSubtask(task, task.Subtasks[s], s);
            }
            else if (task.Subtasks.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(24f);
                GUILayout.Label($"… {task.Subtasks.Count} subtask(s) hidden", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }

        private void DrawSubtask(Task owner, Subtask sub, int index)
        {
            Color tint = CardColors.For(sub.Type);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24f);
            DrawIdBadge($"ST-{owner.Id}.{sub.Id}", tint);

            EditorGUI.BeginChangeCheck();
            var type = (CardType)EditorGUILayout.EnumPopup(sub.Type, GUILayout.Width(74f));
            string desc = EditorGUILayout.TextField(sub.Description);
            var est = (TaskEstimation)EditorGUILayout.EnumPopup(sub.Estimation, GUILayout.Width(80f));
            DrawProgressPopup(ref sub.Progress, out var prog, out bool progChanged);
            if (EditorGUI.EndChangeCheck() || progChanged)
            {
                Undo.RecordObject(_board, "Edit Subtask");
                sub.Type = type;
                sub.Description = desc;
                sub.Estimation = est;
                sub.Progress = prog;
                Persist();
            }

            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("▲", GUILayout.Width(22f)))
                    MoveSubtask(owner, index, index - 1);
            using (new EditorGUI.DisabledScope(index == owner.Subtasks.Count - 1))
                if (GUILayout.Button("▼", GUILayout.Width(22f)))
                    MoveSubtask(owner, index, index + 1);

            GUILayout.Space(34f); // align under +ST column
            if (ColoredButton("X", new Color(0.85f, 0.25f, 0.25f), 22f))
            {
                Undo.RecordObject(_board, "Delete Subtask");
                owner.Subtasks.RemoveAt(index);
                Persist();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        // A small colored chip showing the task/subtask id.
        private void DrawIdBadge(string text, Color color)
        {
            var rect = GUILayoutUtility.GetRect(64f, 18f, GUILayout.Width(64f), GUILayout.Height(18f));
            EditorGUI.DrawRect(rect, color);
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            GUI.Label(rect, text, style);
        }

        // Progress popup colored by state. Returns the (possibly) new value.
        private void DrawProgressPopup(ref TaskProgress current, out TaskProgress value, out bool changed)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = CardColors.For(current);
            EditorGUI.BeginChangeCheck();
            value = (TaskProgress)EditorGUILayout.EnumPopup(current, GUILayout.Width(100f));
            changed = EditorGUI.EndChangeCheck();
            GUI.backgroundColor = prev;
        }

        private bool ColoredButton(string label, Color color, float width)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(label, GUILayout.Width(width));
            GUI.backgroundColor = prev;
            return clicked;
        }

        private void MoveTask(int from, int to)
        {
            Undo.RecordObject(_board, "Reorder Task");
            var item = _board.Tasks[from];
            _board.Tasks.RemoveAt(from);
            _board.Tasks.Insert(to, item);
            Persist();
            GUIUtility.ExitGUI();
        }

        private void MoveSubtask(Task owner, int from, int to)
        {
            Undo.RecordObject(_board, "Reorder Subtask");
            var item = owner.Subtasks[from];
            owner.Subtasks.RemoveAt(from);
            owner.Subtasks.Insert(to, item);
            Persist();
            GUIUtility.ExitGUI();
        }
    }
}
