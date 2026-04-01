using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor
{
    public sealed class SmartCampusQaPanelWindow : EditorWindow
    {
        private enum StatusFilter
        {
            All,
            NotRun,
            Pass,
            Fail,
            Inconclusive
        }

        private readonly Dictionary<string, ProjectQaRunRecord> runRecords = new();
        private readonly Dictionary<string, bool> categoryFoldouts = new();

        private IReadOnlyList<ProjectQaTestCase> tests = Array.Empty<ProjectQaTestCase>();
        private Vector2 testsScroll;
        private Vector2 detailsScroll;
        private string searchText = string.Empty;
        private string selectedTestId;
        private bool isRunning;
        private string activeTestId;
        private StatusFilter statusFilter;
        private GUIStyle richLabelStyle;
        private GUIStyle wrappedMiniLabelStyle;

        [MenuItem("Tools/Smart Campus/QA Panel")]
        private static void OpenWindow()
        {
            var window = GetWindow<SmartCampusQaPanelWindow>(false, SmartCampusProjectQaUtility.ToolName);
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTests();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();
            DrawHeader();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTestsPane();
                DrawDetailsPane();
            }
        }

        private void EnsureStyles()
        {
            if (richLabelStyle == null)
            {
                richLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (wrappedMiniLabelStyle == null)
            {
                wrappedMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true
                };
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var nextSearch = GUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(220f));
                if (!string.Equals(nextSearch, searchText, StringComparison.Ordinal))
                {
                    searchText = nextSearch;
                }

                statusFilter = (StatusFilter)EditorGUILayout.EnumPopup(statusFilter, EditorStyles.toolbarPopup, GUILayout.Width(120f));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(isRunning || tests.Count == 0))
                {
                    if (GUILayout.Button("Run All Tests", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    {
                        RunSequence(tests);
                    }
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    RefreshTests();
                }

                if (GUILayout.Button("Clear Results", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    runRecords.Clear();
                    selectedTestId = null;
                    Repaint();
                }
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(SmartCampusProjectQaUtility.ToolName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Visual hub for cooperative validation, scene flow checks and project QA diagnostics.",
                wrappedMiniLabelStyle);

            if (isRunning && !string.IsNullOrWhiteSpace(activeTestId))
            {
                var activeTest = tests.FirstOrDefault(test => test.Id == activeTestId);
                EditorGUILayout.HelpBox(
                    activeTest == null
                        ? "Running QA tests..."
                        : $"Running: {activeTest.DisplayName}",
                    MessageType.Info);
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawTestsPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.6f)))
            {
                testsScroll = EditorGUILayout.BeginScrollView(testsScroll);

                foreach (var category in tests.GroupBy(test => test.CategoryId).OrderBy(group => group.First().CategoryOrder))
                {
                    DrawCategory(category.ToList());
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCategory(IReadOnlyList<ProjectQaTestCase> categoryTests)
        {
            var visibleTests = categoryTests.Where(MatchesSearchAndFilter).ToList();
            if (visibleTests.Count == 0)
            {
                return;
            }

            var categoryId = categoryTests[0].CategoryId;
            var categoryName = categoryTests[0].CategoryName;
            categoryFoldouts.TryAdd(categoryId, true);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    categoryFoldouts[categoryId] = EditorGUILayout.Foldout(categoryFoldouts[categoryId], categoryName, true);
                    GUILayout.Label(BuildCategorySummary(categoryTests), wrappedMiniLabelStyle);
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(isRunning))
                    {
                        if (GUILayout.Button("Run All", GUILayout.Width(70f)))
                        {
                            RunSequence(categoryTests);
                        }
                    }
                }

                if (!categoryFoldouts[categoryId])
                {
                    return;
                }

                EditorGUILayout.Space(4f);

                foreach (var testCase in visibleTests)
                {
                    DrawTestRow(testCase);
                    EditorGUILayout.Space(4f);
                }
            }
        }

        private void DrawTestRow(ProjectQaTestCase testCase)
        {
            var status = GetStatus(testCase.Id);
            runRecords.TryGetValue(testCase.Id, out var record);
            var statusMarkup = GetStatusMarkup(status);

            using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(statusMarkup, richLabelStyle, GUILayout.Width(96f));

                    if (GUILayout.Button(testCase.DisplayName, EditorStyles.linkLabel))
                    {
                        selectedTestId = testCase.Id;
                    }

                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(isRunning))
                    {
                        if (GUILayout.Button("Run", GUILayout.Width(56f)))
                        {
                            RunSequence(new[] { testCase });
                        }
                    }
                }

                EditorGUILayout.LabelField(
                    record == null ? testCase.Description : record.Summary,
                    wrappedMiniLabelStyle);

                if (record != null)
                {
                    EditorGUILayout.LabelField(
                        $"Last run: {record.Duration.TotalMilliseconds:0} ms",
                        wrappedMiniLabelStyle);
                }
            }
        }

        private void DrawDetailsPane()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
                detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll);

                var selectedTest = tests.FirstOrDefault(test => test.Id == selectedTestId);
                if (selectedTest == null)
                {
                    EditorGUILayout.LabelField(
                        "Select a test to inspect its description, diagnostics and logs.",
                        wrappedMiniLabelStyle);
                    EditorGUILayout.EndScrollView();
                    return;
                }

                EditorGUILayout.LabelField(selectedTest.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(selectedTest.CategoryName, wrappedMiniLabelStyle);
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(selectedTest.Description, wrappedMiniLabelStyle);
                EditorGUILayout.Space(10f);

                if (!runRecords.TryGetValue(selectedTest.Id, out var record))
                {
                    EditorGUILayout.HelpBox("This test has not been run yet.", MessageType.None);
                    EditorGUILayout.EndScrollView();
                    return;
                }

                EditorGUILayout.LabelField($"Status: {GetStatusText(record.Status)}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Duration: {record.Duration.TotalMilliseconds:0} ms", wrappedMiniLabelStyle);
                EditorGUILayout.Space(6f);

                if (!string.IsNullOrWhiteSpace(record.Summary))
                {
                    EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(record.Summary, GUILayout.MinHeight(42f));
                    EditorGUILayout.Space(6f);
                }

                if (!string.IsNullOrWhiteSpace(record.Details))
                {
                    EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(record.Details, GUILayout.MinHeight(92f));
                    EditorGUILayout.Space(6f);
                }

                if (!string.IsNullOrWhiteSpace(record.StackTrace))
                {
                    EditorGUILayout.LabelField("Stack Trace", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(record.StackTrace, GUILayout.MinHeight(92f));
                    EditorGUILayout.Space(6f);
                }

                EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);
                if (record.Logs.Count == 0)
                {
                    EditorGUILayout.LabelField("No logs captured during this run.", wrappedMiniLabelStyle);
                }
                else
                {
                    foreach (var log in record.Logs)
                    {
                        EditorGUILayout.TextArea(
                            $"[{log.Type}] {log.Message}\n{log.StackTrace}".Trim(),
                            GUILayout.MinHeight(string.IsNullOrWhiteSpace(log.StackTrace) ? 36f : 74f));
                        EditorGUILayout.Space(4f);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void RunSequence(IEnumerable<ProjectQaTestCase> sequence)
        {
            if (isRunning)
            {
                return;
            }

            RunSequenceAsync(sequence.ToArray());
        }

        private async void RunSequenceAsync(IReadOnlyList<ProjectQaTestCase> sequence)
        {
            isRunning = true;
            try
            {
                foreach (var testCase in sequence)
                {
                    activeTestId = testCase.Id;
                    selectedTestId = testCase.Id;
                    Repaint();
                    runRecords[testCase.Id] = await ProjectQaTestRunner.RunAsync(testCase);
                    Repaint();
                }
            }
            finally
            {
                activeTestId = null;
                isRunning = false;
                Repaint();
            }
        }

        private void RefreshTests()
        {
            ProjectQaTestRegistry.Refresh();
            tests = ProjectQaTestRegistry.GetTests();
            foreach (var categoryId in tests.Select(test => test.CategoryId).Distinct())
            {
                categoryFoldouts.TryAdd(categoryId, true);
            }

            Repaint();
        }

        private bool MatchesSearchAndFilter(ProjectQaTestCase testCase)
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var matchesSearch = testCase.DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    testCase.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    testCase.CategoryName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchesSearch)
                {
                    return false;
                }
            }

            if (statusFilter == StatusFilter.All)
            {
                return true;
            }

            var status = GetStatus(testCase.Id);
            return statusFilter switch
            {
                StatusFilter.NotRun => status == ProjectQaTestStatus.NotRun,
                StatusFilter.Pass => status == ProjectQaTestStatus.Pass,
                StatusFilter.Fail => status == ProjectQaTestStatus.Fail,
                StatusFilter.Inconclusive => status == ProjectQaTestStatus.Inconclusive,
                _ => true
            };
        }

        private ProjectQaTestStatus GetStatus(string testId)
        {
            if (string.Equals(activeTestId, testId, StringComparison.Ordinal))
            {
                return ProjectQaTestStatus.Running;
            }

            return runRecords.TryGetValue(testId, out var record) ? record.Status : ProjectQaTestStatus.NotRun;
        }

        private string BuildCategorySummary(IReadOnlyList<ProjectQaTestCase> categoryTests)
        {
            var passCount = categoryTests.Count(test => GetStatus(test.Id) == ProjectQaTestStatus.Pass);
            var failCount = categoryTests.Count(test => GetStatus(test.Id) == ProjectQaTestStatus.Fail);
            var inconclusiveCount = categoryTests.Count(test => GetStatus(test.Id) == ProjectQaTestStatus.Inconclusive);
            var notRunCount = categoryTests.Count(test => GetStatus(test.Id) == ProjectQaTestStatus.NotRun);
            return $"Pass {passCount} | Fail {failCount} | Inconclusive {inconclusiveCount} | Not run {notRunCount}";
        }

        private string GetStatusMarkup(ProjectQaTestStatus status)
        {
            return status switch
            {
                ProjectQaTestStatus.Running => "<color=#64B5F6>RUNNING</color>",
                ProjectQaTestStatus.Pass => "<color=#66BB6A>PASS</color>",
                ProjectQaTestStatus.Fail => "<color=#EF5350>FAIL</color>",
                ProjectQaTestStatus.Inconclusive => "<color=#FFB74D>INCONCLUSIVE</color>",
                _ => "<color=#B0BEC5>NOT RUN</color>"
            };
        }

        private string GetStatusText(ProjectQaTestStatus status)
        {
            return status switch
            {
                ProjectQaTestStatus.Running => "Running",
                ProjectQaTestStatus.Pass => "Pass",
                ProjectQaTestStatus.Fail => "Fail",
                ProjectQaTestStatus.Inconclusive => "Inconclusive",
                _ => "Not run"
            };
        }
    }
}
