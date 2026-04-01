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
                "encina,Encina,Carrasca|Quercus ilex,,Perenne,Pequeña,1,Coriácea,3,Bellota,Seco\n" +
                "olmo,Olmo,Ulmus minor,,Caduca,Grande,3,Áspera,2,Samara,Seco\n";

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
                "encina,Encina,, ,Perenne,Pequeña,1,Coriácea,3,Bellota,Seco\n" +
                "encina,Olmo,, ,Caduca,Grande,3,Áspera,2,Samara,Seco\n";

            var parsed = CollaborativePlantGuessCsvService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("plantId repetido"));
        }
    }
}
