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
                new("madrono", "Madrono", new[] { "Arbutus unedo" }, string.Empty, "Perenne", "Mediana", 2, "Lisa", 1, "Baya", "Carnoso"),
                new("olmo", "Olmo", new[] { "Ulmus minor" }, string.Empty, "Caduca", "Grande", 3, "Aspera", 2, "Samara", "Seco"),
                new("encina", "Encina", new[] { "Carrasca" }, string.Empty, "Perenne", "Pequena", 1, "Coriacea", 3, "Bellota", "Seco")
            };

            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(definitions, "madró", 5);

            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].DisplayName, Is.EqualTo("Madrono"));
        }

        [Test]
        public void TryResolvePlant_MatchesAlias()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                new("encina", "Encina", new[] { "Carrasca" }, string.Empty, "Perenne", "Pequena", 1, "Coriacea", 3, "Bellota", "Seco")
            };

            var resolved = CollaborativePlantGuessAutocompleteService.TryResolvePlant(definitions, "carrasca", out var plantDefinition);

            Assert.That(resolved, Is.True);
            Assert.That(plantDefinition.PlantId, Is.EqualTo("encina"));
        }

        [Test]
        public void BuildSuggestions_PrefixMatchesAppearBeforeContainsMatches_AndRespectLimit()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                new("laurel", "Laurel", new[] { "Laurus nobilis" }, string.Empty, "Perenne", "Mediana", 2, "Lisa", 1, "Drupa", "Carnoso"),
                new("madrono", "Madrono", new[] { "Arbutus unedo" }, string.Empty, "Perenne", "Mediana", 2, "Lisa", 1, "Baya", "Carnoso"),
                new("salvia", "Salvia", new[] { "La salvia comun" }, string.Empty, "Perenne", "Pequena", 1, "Lisa", 1, "Aquenio", "Seco")
            };

            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(definitions, "lau", 2);

            Assert.That(suggestions.Count, Is.EqualTo(2));
            Assert.That(suggestions[0].DisplayName, Is.EqualTo("Laurel"));
            Assert.That(suggestions[1].DisplayName, Is.EqualTo("Salvia"));
        }

        [Test]
        public void TryResolvePlant_DoesNotResolvePartialName()
        {
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                new("encina", "Encina", new[] { "Carrasca" }, string.Empty, "Perenne", "Pequena", 1, "Coriacea", 3, "Bellota", "Seco")
            };

            var resolved = CollaborativePlantGuessAutocompleteService.TryResolvePlant(definitions, "enci", out _);

            Assert.That(resolved, Is.False);
        }
    }
}
