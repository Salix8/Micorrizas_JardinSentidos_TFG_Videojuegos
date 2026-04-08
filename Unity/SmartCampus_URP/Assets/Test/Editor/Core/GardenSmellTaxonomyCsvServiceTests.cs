using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenSmellTaxonomy;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenSmellTaxonomyCsvServiceTests
    {
        [Test]
        public void TryParse_ValidCsv_BuildsDefinitions()
        {
            const string csv =
                "plantId,scientificName,imagePath,correctCategory\n" +
                "lavanda,Lavandula dentata,Plants/lavandula.png,Decoracion\n" +
                "menta,Mentha spicata,Plants/mentha.png,Alimentacion\n" +
                "aloe,Aloe vera,Plants/aloe.png,Curacion\n";

            var parsed = GardenSmellTaxonomyCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(3));
            Assert.That(definitions[0].ScientificName, Is.EqualTo("Lavandula dentata"));
            Assert.That(definitions[1].CorrectCategory, Is.EqualTo(GardenSmellTaxonomyCategory.Food));
        }

        [Test]
        public void TryParse_InvalidCategory_ReturnsFalse()
        {
            const string csv =
                "plantId,scientificName,imagePath,correctCategory\n" +
                "lavanda,Lavandula dentata,Plants/lavandula.png,Aroma\n" +
                "menta,Mentha spicata,Plants/mentha.png,Alimentacion\n" +
                "aloe,Aloe vera,Plants/aloe.png,Curacion\n";

            var parsed = GardenSmellTaxonomyCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("categoria no valida"));
        }

        [Test]
        public void TryParse_DuplicatePlantId_ReturnsFalse()
        {
            const string csv =
                "plantId,scientificName,imagePath,correctCategory\n" +
                "lavanda,Lavandula dentata,Plants/lavandula.png,Decoracion\n" +
                "lavanda,Mentha spicata,Plants/mentha.png,Alimentacion\n" +
                "aloe,Aloe vera,Plants/aloe.png,Curacion\n";

            var parsed = GardenSmellTaxonomyCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("plantId repetido"));
        }
    }
}
