using NUnit.Framework;
using SmartCampus.Dialogue;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueCsvServiceTests
    {
        [Test]
        public void TryParse_ValidLocalizationCsv_BuildsOrderedDialogueLines()
        {
            const string csv =
                "String ID,Character,Act/Location,Context/Notes,Spanish (es-ES),English (en-US),Catalan (ca-CA),,Mis cambios\n" +
                "DL_01,Deeproot,Act I,Intro,\"Hola, caminante.\",Hello wanderer.,Hola caminant.,,\n" +
                ",,,,,,,,\n" +
                "DL_02,Deeproot,Act I,Pista,\"Busca en las raices.\",Search in the roots.,Busca en les arrels.,,\n";

            var parsed = DialogueCsvService.TryParse(csv, out var lines, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0].StringId, Is.EqualTo("DL_01"));
            Assert.That(lines[0].Character, Is.EqualTo("Deeproot"));
            Assert.That(lines[0].ActOrLocation, Is.EqualTo("Act I"));
            Assert.That(lines[0].TryGetText("es-ES", "en-US", out var text), Is.True);
            Assert.That(text, Is.EqualTo("Hola, caminante."));
        }

        [Test]
        public void TryParse_DuplicateStringId_ReturnsFalse()
        {
            const string csv =
                "String ID,Character,Spanish (es-ES)\n" +
                "DL_01,Deeproot,Hola\n" +
                "DL_01,Deeproot,Otra linea\n";

            var parsed = DialogueCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("String ID repetido"));
        }

        [Test]
        public void TryParse_MissingLocaleColumns_ReturnsFalse()
        {
            const string csv =
                "String ID,Character,Act/Location\n" +
                "DL_01,Deeproot,Act I\n";

            var parsed = DialogueCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("columna de idioma"));
        }

        [Test]
        public void DialogueLine_TryGetText_UsesFallbackLocaleWhenRequestedLocaleIsEmpty()
        {
            const string csv =
                "String ID,Character,Spanish (es-ES),English (en-US)\n" +
                "DL_01,Deeproot,,Fallback text\n";

            var parsed = DialogueCsvService.TryParse(csv, out var lines, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(lines[0].TryGetText("es-ES", "en-US", out var text), Is.True);
            Assert.That(text, Is.EqualTo("Fallback text"));
        }
    }
}
