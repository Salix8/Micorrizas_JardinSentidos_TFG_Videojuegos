using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor
{
    public static class ProjectQaTestRegistry
    {
        private static IReadOnlyList<ProjectQaTestCase> cachedTests;

        public static IReadOnlyList<ProjectQaTestCase> GetTests()
        {
            if (cachedTests == null)
            {
                cachedTests = TypeCache.GetTypesDerivedFrom<ProjectQaTestCase>()
                    .Where(type => !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                    .Select(type => (ProjectQaTestCase)Activator.CreateInstance(type))
                    .OrderBy(test => test.CategoryOrder)
                    .ThenBy(test => test.CategoryName)
                    .ThenBy(test => test.Order)
                    .ThenBy(test => test.DisplayName)
                    .ToArray();
            }

            return cachedTests;
        }

        public static void Refresh()
        {
            cachedTests = null;
        }
    }

    public static class ProjectQaTestRunner
    {
        public static async Task<ProjectQaRunRecord> RunAsync(ProjectQaTestCase testCase, CancellationToken cancellationToken = default)
        {
            var context = new ProjectQaExecutionContext(cancellationToken);
            var startedAtUtc = DateTime.UtcNow;

            void HandleUnityLog(string condition, string stackTrace, LogType type)
            {
                context.CaptureUnityLog(type, condition, stackTrace);
            }

            Application.logMessageReceived += HandleUnityLog;
            try
            {
                var outcome = await testCase.ExecuteAsync(context) ?? ProjectQaOutcome.Inconclusive("The QA test finished without returning a result.");
                return new ProjectQaRunRecord(
                    testCase.Id,
                    outcome.Status,
                    outcome.Summary,
                    outcome.Details,
                    outcome.StackTrace,
                    context.Logs,
                    startedAtUtc,
                    DateTime.UtcNow);
            }
            catch (OperationCanceledException exception)
            {
                context.CaptureUnityLog(LogType.Warning, exception.Message, exception.StackTrace);
                return new ProjectQaRunRecord(
                    testCase.Id,
                    ProjectQaTestStatus.Inconclusive,
                    "The QA test was cancelled.",
                    exception.Message,
                    exception.StackTrace,
                    context.Logs,
                    startedAtUtc,
                    DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                context.CaptureUnityLog(LogType.Exception, exception.Message, exception.StackTrace);
                return new ProjectQaRunRecord(
                    testCase.Id,
                    ProjectQaTestStatus.Fail,
                    "The QA test threw an unhandled exception.",
                    exception.Message,
                    exception.StackTrace,
                    context.Logs,
                    startedAtUtc,
                    DateTime.UtcNow);
            }
            finally
            {
                Application.logMessageReceived -= HandleUnityLog;
            }
        }
    }
}
