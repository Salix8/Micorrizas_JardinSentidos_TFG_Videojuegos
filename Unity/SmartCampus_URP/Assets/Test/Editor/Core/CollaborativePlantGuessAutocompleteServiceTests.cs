using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessAutocompleteServiceTests
    {
        private static CollaborativePlantGuessPlantDefinition CreatePlant(
            string plantId,
            string commonName,
            string scientificName,
            string[] synonyms = null)
        {
            return new CollaborativePlantGuessPlantDefinition(
                plantId,
                commonName,
                scientificName,
                synonyms ?? new string[0],
                string.Empty,
                "Arbol",
                "Media",
                "Simple",
                "Carnoso",
                "Baya");
        }

        [Test]
        public void BuildSuggestions_PrioritizesPrefixAndIgnoresAccents()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                CreatePlant("madrono", "Madrono", "Arbutus unedo", synonyms: new[] { "Arbuto" }),
                CreatePlant("olmo", "Olmo", "Ulmus minor"),
                CreatePlant("encina", "Encina", "Quercus ilex", synonyms: new[] { "Carrasca" })
            };

            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(definitions, "madró", 5);

            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].DisplayName, Is.EqualTo("Madrono"));
        }

        [Test]
        public void TryResolvePlant_MatchesSynonym()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                CreatePlant("encina", "Encina", "Quercus ilex", synonyms: new[] { "Carrasca" })
            };

            var resolved = CollaborativePlantGuessAutocompleteService.TryResolvePlant(definitions, "carrasca", out var plantDefinition);

            Assert.That(resolved, Is.True);
            Assert.That(plantDefinition.PlantId, Is.EqualTo("encina"));
        }

        [Test]
        public void BuildSuggestions_ReturnsOnlyPrefixMatches_AndRespectLimit()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                CreatePlant("laurel", "Laurel", "Laurus nobilis"),
                CreatePlant("madrono", "Madrono", "Arbutus unedo"),
                CreatePlant("salvia", "Salvia", "Salvia officinalis", synonyms: new[] { "La salvia comun" })
            };

            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(definitions, "lau", 2);

            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].DisplayName, Is.EqualTo("Laurel"));
        }

        [Test]
        public void TryResolvePlant_ResolvesUniqueScientificPrefix()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                CreatePlant("olivo", "Olivo", "Olea europaea"),
                CreatePlant("encina", "Encina", "Quercus ilex")
            };

            var resolved = CollaborativePlantGuessAutocompleteService.TryResolvePlant(definitions, "olea", out var plantDefinition);

            Assert.That(resolved, Is.True);
            Assert.That(plantDefinition.PlantId, Is.EqualTo("olivo"));
        }

        [Test]
        public void TryResolvePlant_DoesNotResolveAmbiguousPrefix()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                CreatePlant("olivo", "Olivo", "Olea europaea"),
                CreatePlant("olmo", "Olmo", "Ulmus minor")
            };

            var resolved = CollaborativePlantGuessAutocompleteService.TryResolvePlant(definitions, "ol", out _);

            Assert.That(resolved, Is.False);
        }
    }
}
