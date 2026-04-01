using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessAutocompleteServiceTests
    {
        [Test]
        public void BuildSuggestions_PrioritizesPrefixAndIgnoresAccents()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                new("madrono", "Madroño", new[] { "Arbutus unedo" }, string.Empty, "Perenne", "Mediana", 2, "Lisa", 1, "Baya", "Carnoso"),
                new("olmo", "Olmo", new[] { "Ulmus minor" }, string.Empty, "Caduca", "Grande", 3, "Áspera", 2, "Samara", "Seco"),
                new("encina", "Encina", new[] { "Carrasca" }, string.Empty, "Perenne", "Pequeña", 1, "Coriácea", 3, "Bellota", "Seco")
            };

            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(definitions, "madro", 5);

            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].DisplayName, Is.EqualTo("Madroño"));
        }

        [Test]
        public void TryResolvePlant_MatchesAlias()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                new("encina", "Encina", new[] { "Carrasca" }, string.Empty, "Perenne", "Pequeña", 1, "Coriácea", 3, "Bellota", "Seco")
            };

            var resolved = CollaborativePlantGuessAutocompleteService.TryResolvePlant(definitions, "carrasca", out var plantDefinition);

            Assert.That(resolved, Is.True);
            Assert.That(plantDefinition.PlantId, Is.EqualTo("encina"));
        }
    }
}
