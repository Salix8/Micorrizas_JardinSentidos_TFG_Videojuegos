using UnityEngine;

public sealed class CoopMinigameZoneCountdownTracker
{
    private int currentZoneId = -1;
    private float countdownStartTime;
    private bool isActive;
    private bool isCompleted;

    public CoopMinigameZoneCountdownSnapshot Update(int zoneId, bool hasCandidate, float currentTime, float durationSeconds)
    {
        if (!hasCandidate || durationSeconds <= 0f)
        {
            Reset();
            return CoopMinigameZoneCountdownSnapshot.Inactive;
        }

        if (!isActive || currentZoneId != zoneId)
        {
            currentZoneId = zoneId;
            countdownStartTime = currentTime;
            isActive = true;
            isCompleted = false;
        }

        var elapsedSeconds = Mathf.Max(0f, currentTime - countdownStartTime);
        var progress01 = Mathf.Clamp01(elapsedSeconds / durationSeconds);
        var remainingSeconds = Mathf.Max(0f, durationSeconds - elapsedSeconds);

        if (progress01 >= 1f)
        {
            isCompleted = true;
        }

        return new CoopMinigameZoneCountdownSnapshot(
            isActive: true,
            isCompleted: isCompleted,
            zoneId: currentZoneId,
            progress01: progress01,
            elapsedSeconds: elapsedSeconds,
            remainingSeconds: remainingSeconds);
    }

    public void Reset()
    {
        currentZoneId = -1;
        countdownStartTime = 0f;
        isActive = false;
        isCompleted = false;
    }
}

public readonly struct CoopMinigameZoneCountdownSnapshot
{
    public static readonly CoopMinigameZoneCountdownSnapshot Inactive = new(
        isActive: false,
        isCompleted: false,
        zoneId: -1,
        progress01: 0f,
        elapsedSeconds: 0f,
        remainingSeconds: 0f);

    public CoopMinigameZoneCountdownSnapshot(
        bool isActive,
        bool isCompleted,
        int zoneId,
        float progress01,
        float elapsedSeconds,
        float remainingSeconds)
    {
        IsActive = isActive;
        IsCompleted = isCompleted;
        ZoneId = zoneId;
        Progress01 = progress01;
        ElapsedSeconds = elapsedSeconds;
        RemainingSeconds = remainingSeconds;
    }

    public bool IsActive { get; }
    public bool IsCompleted { get; }
    public int ZoneId { get; }
    public float Progress01 { get; }
    public float ElapsedSeconds { get; }
    public float RemainingSeconds { get; }
}
