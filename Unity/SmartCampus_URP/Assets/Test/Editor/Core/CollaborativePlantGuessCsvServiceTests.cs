using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessCsvServiceTests
    {
        [Test]
        public void TryParse_ValidCsv_BuildsDefinitionsWithSearchNames()
        {
            const string csv =
                "plantId,commonName,scientificName,synonyms,imagePath,plantType,surfaceRoughness,surfaceRoughnessOrder,leafType,fruitCategory,fruitType\n" +
                "encina,Encina,Quercus ilex,Carrasca|Chaparra,,Arbol,Rugosa,4,Coriacea,Seco,Bellota\n" +
                "olivo,Olivo,Olea europaea,Aceituno,,Arbol,Media,3,Lanceolada,Carnoso,Drupa\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].DisplayName, Is.EqualTo("Encina"));
            Assert.That(definitions[0].ScientificName, Is.EqualTo("Quercus ilex"));
            Assert.That(definitions[0].Synonyms.Count, Is.EqualTo(2));
            Assert.That(definitions[0].FruitCategory, Is.EqualTo("Seco"));
        }

        [Test]
        public void TryParse_LegacyAliasesHeader_IsStillAccepted()
        {
            const string csv =
                "plantId,commonName,scientificName,aliases,imagePath,plantType,surfaceRoughness,surfaceRoughnessOrder,leafType,fruitCategory,fruitType\n" +
                "encina,Encina,Quercus ilex,Carrasca,,Arbol,Rugosa,4,Coriacea,Seco,Bellota\n" +
                "olivo,Olivo,Olea europaea,Aceituno,,Arbol,Media,3,Lanceolada,Carnoso,Drupa\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions[0].Synonyms[0], Is.EqualTo("Carrasca"));
        }

        [Test]
        public void TryParse_DuplicatePlantId_ReturnsFalse()
        {
            const string csv =
                "plantId,commonName,scientificName,synonyms,imagePath,plantType,surfaceRoughness,surfaceRoughnessOrder,leafType,fruitCategory,fruitType\n" +
                "encina,Encina,Quercus ilex,Carrasca,,Arbol,Rugosa,4,Coriacea,Seco,Bellota\n" +
                "encina,Olivo,Olea europaea,Aceituno,,Arbol,Media,3,Lanceolada,Carnoso,Drupa\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("plantId repetido"));
        }

        [Test]
        public void TryParse_MissingNewSchemaColumn_ReturnsFalse()
        {
            const string csv =
                "plantId,commonName,scientificName,synonyms,imagePath,plantType,surfaceRoughness,leafType,fruitCategory,fruitType\n" +
                "encina,Encina,Quercus ilex,Carrasca,,Arbol,Rugosa,Coriacea,Seco,Bellota\n" +
                "olivo,Olivo,Olea europaea,Aceituno,,Arbol,Media,Lanceolada,Carnoso,Drupa\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("surfaceRoughnessOrder"));
        }
    }
}
