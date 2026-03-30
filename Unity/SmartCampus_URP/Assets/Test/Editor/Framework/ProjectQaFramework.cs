using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SmartCampus.Testing
{
    public enum ProjectQaTestStatus
    {
        NotRun,
        Running,
        Pass,
        Fail,
        Inconclusive
    }

    public sealed class ProjectQaLogEntry
    {
        public ProjectQaLogEntry(LogType type, string message, string stackTrace)
        {
            TimestampUtc = DateTime.UtcNow;
            Type = type;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
        }

        public DateTime TimestampUtc { get; }
        public LogType Type { get; }
        public string Message { get; }
        public string StackTrace { get; }
    }

    public sealed class ProjectQaOutcome
    {
        private ProjectQaOutcome(ProjectQaTestStatus status, string summary, string details, string stackTrace)
        {
            Status = status;
            Summary = summary ?? string.Empty;
            Details = details ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
        }

        public ProjectQaTestStatus Status { get; }
        public string Summary { get; }
        public string Details { get; }
        public string StackTrace { get; }

        public static ProjectQaOutcome Pass(string summary, string details = "")
        {
            return new ProjectQaOutcome(ProjectQaTestStatus.Pass, summary, details, string.Empty);
        }

        public static ProjectQaOutcome Fail(string summary, string details = "", string stackTrace = "")
        {
            return new ProjectQaOutcome(ProjectQaTestStatus.Fail, summary, details, stackTrace);
        }

        public static ProjectQaOutcome Inconclusive(string summary, string details = "", string stackTrace = "")
        {
            return new ProjectQaOutcome(ProjectQaTestStatus.Inconclusive, summary, details, stackTrace);
        }
    }

    public sealed class ProjectQaExecutionContext
    {
        private readonly List<ProjectQaLogEntry> logs = new();

        public ProjectQaExecutionContext(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }
        public IReadOnlyList<ProjectQaLogEntry> Logs => logs;

        public void Info(string message)
        {
            AddLog(LogType.Log, message, string.Empty);
        }

        public void Warning(string message)
        {
            AddLog(LogType.Warning, message, string.Empty);
        }

        public void Error(string message, string stackTrace = "")
        {
            AddLog(LogType.Error, message, stackTrace);
        }

        public void CaptureUnityLog(LogType type, string message, string stackTrace)
        {
            AddLog(type, message, stackTrace);
        }

        private void AddLog(LogType type, string message, string stackTrace)
        {
            logs.Add(new ProjectQaLogEntry(type, message, stackTrace));
        }
    }

    public sealed class ProjectQaRunRecord
    {
        public ProjectQaRunRecord(
            string testId,
            ProjectQaTestStatus status,
            string summary,
            string details,
            string stackTrace,
            IReadOnlyList<ProjectQaLogEntry> logs,
            DateTime startedAtUtc,
            DateTime completedAtUtc)
        {
            TestId = testId;
            Status = status;
            Summary = summary ?? string.Empty;
            Details = details ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Logs = logs ?? Array.Empty<ProjectQaLogEntry>();
            StartedAtUtc = startedAtUtc;
            CompletedAtUtc = completedAtUtc;
        }

        public string TestId { get; }
        public ProjectQaTestStatus Status { get; }
        public string Summary { get; }
        public string Details { get; }
        public string StackTrace { get; }
        public IReadOnlyList<ProjectQaLogEntry> Logs { get; }
        public DateTime StartedAtUtc { get; }
        public DateTime CompletedAtUtc { get; }
        public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;
    }

    public abstract class ProjectQaTestCase
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string CategoryId { get; }
        public abstract string CategoryName { get; }
        public virtual string Description => string.Empty;
        public virtual int CategoryOrder => 0;
        public virtual int Order => 0;

        public abstract Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context);
    }
}
