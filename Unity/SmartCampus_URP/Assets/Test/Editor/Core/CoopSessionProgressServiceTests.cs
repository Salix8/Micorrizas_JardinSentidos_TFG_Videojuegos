using NUnit.Framework;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CoopSessionProgressServiceTests
    {
        [Test]
        public void CreateDefaultStates_StartsWithAllMinigamesPending()
        {
            var states = CoopSessionProgressService.CreateDefaultStates(5);

            Assert.That(states, Has.Count.EqualTo(5));
            Assert.That(CoopSessionProgressService.CountCompleted(states), Is.EqualTo(0));
            Assert.That(CoopSessionProgressService.AreAllCompleted(states), Is.False);
            Assert.That(CoopSessionProgressService.CalculateAverageScore(states), Is.EqualTo(0f));
        }

        [Test]
        public void RegisteringFiveResults_ComputesTheAverageScore()
        {
            var states = CoopSessionProgressService.CreateDefaultStates(5);
            var scores = new[] { 10f, 8f, 6f, 9f, 7f };

            for (var index = 0; index < scores.Length; index++)
            {
                var state = states[index];
                var updated = CoopSessionProgressService.TryRegisterResult(
                    ref state,
                    index,
                    new MinigameResultData($"Minijuego {index + 1}", scores[index], index + 1, 0),
                    index);
                states[index] = state;

                Assert.That(updated, Is.True);
            }

            Assert.That(CoopSessionProgressService.CountCompleted(states), Is.EqualTo(5));
            Assert.That(CoopSessionProgressService.AreAllCompleted(states), Is.True);
            Assert.That(CoopSessionProgressService.CalculateAverageScore(states), Is.EqualTo(8f));
        }

        [Test]
        public void FivePlayableMinigames_IgnoresReserveStateForCompletionAndAverage()
        {
            var states = CoopSessionProgressService.CreateDefaultStates(6);
            var scores = new[] { 10f, 8f, 6f, 9f, 7f };

            for (var index = 0; index < scores.Length; index++)
            {
                var state = states[index];
                var updated = CoopSessionProgressService.TryRegisterResult(
                    ref state,
                    index,
                    new MinigameResultData($"Minijuego {index + 1}", scores[index], index + 1, 0),
                    index);
                states[index] = state;

                Assert.That(updated, Is.True);
            }

            Assert.That(CoopSessionProgressService.CountCompleted(states, playableMinigameCount: 5), Is.EqualTo(5));
            Assert.That(CoopSessionProgressService.AreAllCompleted(states, playableMinigameCount: 5), Is.True);
            Assert.That(CoopSessionProgressService.CalculateAverageScore(states, playableMinigameCount: 5), Is.EqualTo(8f));
            Assert.That(CoopSessionProgressService.AreAllCompleted(states), Is.False);
        }

        [TestCase(4.99f, false)]
        [TestCase(5f, true)]
        public void RegisterResult_OnlyPassingScoresCompleteMission(float score, bool passes)
        {
            var state = CoopSessionProgressService.CreateDefaultState(0);
            var registered = CoopSessionProgressService.TryRegisterResult(
                ref state, 0, new MinigameResultData("Resultado", score, 1, 1), 0);

            Assert.That(registered, Is.EqualTo(passes));
            Assert.That(state.IsCompleted, Is.EqualTo(passes));
            Assert.That(CoopSessionProgressService.CountCompleted(new[] { state }), Is.EqualTo(passes ? 1 : 0));
        }

        [Test]
        public void FailedAttempts_FollowedByPass_OnlyPassIsRecorded()
        {
            var state = CoopSessionProgressService.CreateDefaultState(0);
            Assert.That(CoopSessionProgressService.TryRegisterResult(ref state, 0, new MinigameResultData("Fallo", 2f, 0, 1), 0), Is.False);
            Assert.That(CoopSessionProgressService.TryRegisterResult(ref state, 0, new MinigameResultData("Fallo", 4f, 0, 1), 0), Is.False);
            Assert.That(CoopSessionProgressService.TryRegisterResult(ref state, 0, new MinigameResultData("Aprobado", 6f, 1, 0), 0), Is.True);
            Assert.That(state.ScoreOutOfTen, Is.EqualTo(6f));
            Assert.That(state.CompletionOrder, Is.Zero);
        }

        [TestCase(100d, 75d, 25d)]
        [TestCase(100d, 100d, 0d)]
        [TestCase(100d, 110d, 0d)]
        public void RetryRemaining_UsesServerDeadline(double deadline, double now, double expected)
        {
            Assert.That(CoopSessionProgressService.GetRetryRemaining(deadline, now), Is.EqualTo(expected));
        }

        [Test]
        public void RegisteringTheSameMinigameTwice_PreservesTheFirstResult()
        {
            var states = CoopSessionProgressService.CreateDefaultStates(1);
            var state = states[0];
            var firstRegistration = CoopSessionProgressService.TryRegisterResult(
                ref state,
                0,
                new MinigameResultData("Primera", 8.5f, 3, 1),
                completionOrder: 0);
            states[0] = state;

            state = states[0];
            var secondRegistration = CoopSessionProgressService.TryRegisterResult(
                ref state,
                0,
                new MinigameResultData("Segunda", 4f, 1, 5),
                completionOrder: 1);
            states[0] = state;

            Assert.That(firstRegistration, Is.True);
            Assert.That(secondRegistration, Is.False);
            Assert.That(states[0].ScoreOutOfTen, Is.EqualTo(8.5f));
            Assert.That(states[0].ResultMessage.ToString(), Is.EqualTo("Primera"));
            Assert.That(states[0].CompletionOrder, Is.EqualTo(0));
        }
    }
}
