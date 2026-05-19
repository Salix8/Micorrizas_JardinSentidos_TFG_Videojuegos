using NUnit.Framework;
using SmartCampus.Dialogue;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueCsvServiceTests
    {
        [Test]
        public void TryParse_ValidCsv_PreservesRowOrderAndLocalizedTexts()
        {
            const string csv =
                "String ID,Character,Act/Location,Context/Notes,Spanish (es-ES),,English (en-US),,Catalan (ca-CA),\n" +
                "DL_002,Deeproot,Act I,Nota,\"Linea, dos\",0,\"Line two\",0,\"Linia dos\",0\n" +
                "DL_001,Deeproot,Act I,Nota,Linea uno,0,Line one,0,Linia u,0\n";

            var parsed = DialogueCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].StringId, Is.EqualTo("DL_002"));
            Assert.That(definitions[0].GetText(DialogueLanguage.Spanish), Is.EqualTo("Linea, dos"));
            Assert.That(definitions[1].GetText(DialogueLanguage.Valencian), Is.EqualTo("Linia u"));
        }

        [Test]
        public void TryParse_DuplicateStringId_ReturnsFalse()
        {
            const string csv =
                "String ID,Character,Act/Location,Context/Notes,Spanish (es-ES),,English (en-US),,Catalan (ca-CA),\n" +
                "DL_001,Deeproot,Act I,Nota,Linea uno,0,Line one,0,Linia u,0\n" +
                "DL_001,Deeproot,Act I,Nota,Linea repetida,0,Repeated line,0,Linia repetida,0\n";

            var parsed = DialogueCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("String ID repetido"));
        }

        [Test]
        public void TryParse_MissingRequiredColumn_ReturnsFalse()
        {
            const string csv =
                "String ID,Character,Act/Location,Spanish (es-ES),,English (en-US),,Catalan (ca-CA),\n" +
                "DL_001,Deeproot,Act I,Linea uno,0,Line one,0,Linia u,0\n";

            var parsed = DialogueCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("Context/Notes"));
        }

        [Test]
        public void TryParse_RowWithoutLocalizedText_ReturnsFalse()
        {
            const string csv =
                "String ID,Character,Act/Location,Context/Notes,Spanish (es-ES),,English (en-US),,Catalan (ca-CA),\n" +
                "DL_001,Deeproot,Act I,Nota,,0,,0,,0\n";

            var parsed = DialogueCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("no contiene texto localizable"));
        }
    }
}
