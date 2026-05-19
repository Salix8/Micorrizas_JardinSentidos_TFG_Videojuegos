using NUnit.Framework;
using SmartCampus.Dialogue;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueCatalogServiceTests
    {
        [Test]
        public void TryCreate_ValidCsv_BuildsSequencesAndLineIndex()
        {
            const string csv =
                "String ID,Character,Act/Location,Context/Notes,Spanish (es-ES),,English (en-US),,Catalan (ca-CA),\n" +
                "DL_001,Deeproot,Act I,Intro,Linea uno,0,Line one,0,Linia u,0\n" +
                "DL_002,Deeproot,Act I,Intro,Linea dos,0,Line two,0,Linia dos,0\n" +
                "DL_003,Deeproot,Act II,Outro,Linea tres,0,Line three,0,Linia tres,0\n";

            var created = DialogueCatalogService.TryCreate(csv, out var catalog, out var errorMessage);

            Assert.That(created, Is.True, errorMessage);
            Assert.That(catalog.SequenceKeys.Count, Is.EqualTo(2));
            Assert.That(catalog.SequenceKeys[0], Is.EqualTo("Act I"));
            Assert.That(catalog.TryGetSequence("Act I", out var actOneSequence), Is.True);
            Assert.That(actOneSequence.Lines.Count, Is.EqualTo(2));
            Assert.That(actOneSequence.Lines[1].StringId, Is.EqualTo("DL_002"));
            Assert.That(catalog.TryGetLine("DL_003", out var line), Is.True);
            Assert.That(line.SequenceKey, Is.EqualTo("Act II"));
        }

        [Test]
        public void TryBuildSingleLineSequence_ExistingLine_ReturnsSingleLineWrapper()
        {
            const string csv =
                "String ID,Character,Act/Location,Context/Notes,Spanish (es-ES),,English (en-US),,Catalan (ca-CA),\n" +
                "DL_001,Deeproot,Act I,Intro,Linea uno,0,Line one,0,Linia u,0\n";

            var created = DialogueCatalogService.TryCreate(csv, out var catalog, out var errorMessage);

            Assert.That(created, Is.True, errorMessage);
            Assert.That(catalog.TryBuildSingleLineSequence("DL_001", out var sequence), Is.True);
            Assert.That(sequence.Key, Is.EqualTo("DL_001"));
            Assert.That(sequence.Lines.Count, Is.EqualTo(1));
            Assert.That(sequence.Lines[0].StringId, Is.EqualTo("DL_001"));
        }
    }
}
