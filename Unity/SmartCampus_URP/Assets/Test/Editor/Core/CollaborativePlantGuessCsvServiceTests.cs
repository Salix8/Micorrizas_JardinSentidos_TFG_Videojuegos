using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessCsvServiceTests
    {
        [Test]
        public void TryParse_ValidCsv_BuildsDefinitionsAndAliases()
        {
            const string csv =
                "plantId,displayName,aliases,imagePath,leafPersistence,leafSize,leafSizeOrder,leafTexture,leafTextureOrder,fruitType,fruitCategory\n" +
                "encina,Encina,Carrasca|Quercus ilex,,Perenne,Pequena,1,Coriacea,3,Bellota,Seco\n" +
                "olmo,Olmo,Ulmus minor,,Caduca,Grande,3,Aspera,2,Samara,Seco\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].DisplayName, Is.EqualTo("Encina"));
            Assert.That(definitions[0].Aliases.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryParse_DuplicatePlantId_ReturnsFalse()
        {
            const string csv =
                "plantId,displayName,aliases,imagePath,leafPersistence,leafSize,leafSizeOrder,leafTexture,leafTextureOrder,fruitType,fruitCategory\n" +
                "encina,Encina,,,Perenne,Pequena,1,Coriacea,3,Bellota,Seco\n" +
                "encina,Olmo,,,Caduca,Grande,3,Aspera,2,Samara,Seco\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("plantId repetido"));
        }

        [Test]
        public void TryParse_BlankFruitCategoryFallsBackToFruitType_AndAliasesAreDeduplicated()
        {
            const string csv =
                "plantId,displayName,aliases,imagePath,leafPersistence,leafSize,leafSizeOrder,leafTexture,leafTextureOrder,fruitType,fruitCategory\n" +
                "encina,Encina,Carrasca|carrasca|Quercus ilex,,Perenne,Pequena,1,Coriacea,3,Bellota,\n" +
                "olmo,Olmo,Ulmus minor,,Caduca,Grande,3,Aspera,2,Samara,Seco\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions[0].Aliases.Count, Is.EqualTo(2));
            Assert.That(definitions[0].FruitCategory, Is.EqualTo("Bellota"));
        }

        [Test]
        public void TryParse_WithSingleValidPlant_ReturnsFalse()
        {
            const string csv =
                "plantId,displayName,aliases,imagePath,leafPersistence,leafSize,leafSizeOrder,leafTexture,leafTextureOrder,fruitType,fruitCategory\n" +
                "encina,Encina,Carrasca,,Perenne,Pequena,1,Coriacea,3,Bellota,Seco\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("al menos dos plantas"));
        }
    }
}
