using NUnit.Framework;

public sealed class CoopMinigameZoneCountdownTrackerTests
{
    [Test]
    public void Update_StartsNewCountdown_WhenCandidateAppears()
    {
        var tracker = new CoopMinigameZoneCountdownTracker();

        var snapshot = tracker.Update(zoneId: 11, hasCandidate: true, currentTime: 5f, durationSeconds: 2.5f);

        Assert.That(snapshot.IsActive, Is.True);
        Assert.That(snapshot.ZoneId, Is.EqualTo(11));
        Assert.That(snapshot.Progress01, Is.EqualTo(0f).Within(0.001f));
        Assert.That(snapshot.IsCompleted, Is.False);
    }

    [Test]
    public void Update_CompletesCountdown_WhenDurationElapses()
    {
        var tracker = new CoopMinigameZoneCountdownTracker();
        tracker.Update(zoneId: 3, hasCandidate: true, currentTime: 1f, durationSeconds: 2f);

        var snapshot = tracker.Update(zoneId: 3, hasCandidate: true, currentTime: 3.1f, durationSeconds: 2f);

        Assert.That(snapshot.IsActive, Is.True);
        Assert.That(snapshot.IsCompleted, Is.True);
        Assert.That(snapshot.Progress01, Is.EqualTo(1f).Within(0.001f));
        Assert.That(snapshot.RemainingSeconds, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void Update_ResetsCountdown_WhenCandidateDisappears()
    {
        var tracker = new CoopMinigameZoneCountdownTracker();
        tracker.Update(zoneId: 7, hasCandidate: true, currentTime: 0f, durationSeconds: 2f);

        var snapshot = tracker.Update(zoneId: -1, hasCandidate: false, currentTime: 0.5f, durationSeconds: 2f);

        Assert.That(snapshot.IsActive, Is.False);
        Assert.That(snapshot.ZoneId, Is.EqualTo(-1));
        Assert.That(snapshot.Progress01, Is.EqualTo(0f).Within(0.001f));
    }
}
