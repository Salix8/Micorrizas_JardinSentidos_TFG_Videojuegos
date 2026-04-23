using NUnit.Framework;
using SmartCampus.Dialogue;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueSequenceServiceTests
    {
        [Test]
        public void MoveNext_AtLastLine_CompletesSequence()
        {
            var completed = false;
            var service = new DialogueSequenceService();
            service.SequenceCompleted += () => completed = true;
            service.Start(new[]
            {
                new DialogueLine("DL_01", "Deeproot", "Act I", string.Empty, new System.Collections.Generic.Dictionary<string, string>
                {
                    { "es-ES", "Hola" }
                })
            });

            var moved = service.MoveNext();

            Assert.That(moved, Is.False);
            Assert.That(completed, Is.True);
            Assert.That(service.IsPlaying, Is.False);
        }

        [Test]
        public void MovePrevious_FromSecondLine_ReturnsFirstLine()
        {
            var service = new DialogueSequenceService();
            service.Start(new[]
            {
                new DialogueLine("DL_01", "Deeproot", "Act I", string.Empty, new System.Collections.Generic.Dictionary<string, string>
                {
                    { "es-ES", "Primera" }
                }),
                new DialogueLine("DL_02", "Deeproot", "Act I", string.Empty, new System.Collections.Generic.Dictionary<string, string>
                {
                    { "es-ES", "Segunda" }
                })
            });

            service.MoveNext();
            var movedPrevious = service.MovePrevious();

            Assert.That(movedPrevious, Is.True);
            Assert.That(service.CurrentSnapshot.CurrentLine.StringId, Is.EqualTo("DL_01"));
        }
    }
}
