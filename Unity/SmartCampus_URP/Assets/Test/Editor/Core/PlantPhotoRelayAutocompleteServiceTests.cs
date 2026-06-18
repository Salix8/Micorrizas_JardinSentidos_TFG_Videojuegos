using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.PlantPhotoRelay;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class PlantPhotoRelayAutocompleteServiceTests
    {
        private static PlantPhotoRelayPlantDefinition CreatePlant(string canonical, string display, params string[] variants)
        {
            return new PlantPhotoRelayPlantDefinition(canonical, display, variants, "Arbol", "rugosa", false, true, "simple", "mediano");
        }

        [Test]
        public void BuildSuggestions_IgnoresAccentsAndCase()
        {
            var definitions = new List<PlantPhotoRelayPlantDefinition>
            {
                CreatePlant("olivo", "Olivo", "Olivera"),
                CreatePlant("encina", "Encina", "Carrasca")
            };

            var suggestions = PlantPhotoRelayAutocompleteService.BuildSuggestions(definitions, "olÍ", 5);

            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].CommonNameCanonical, Is.EqualTo("olivo"));
        }

        [Test]
        public void TryResolvePlant_ResolvesVariant()
        {
            var definitions = new List<PlantPhotoRelayPlantDefinition>
            {
                CreatePlant("olivo", "Olivo", "Olivera", "Aceituno")
            };

            var resolved = PlantPhotoRelayAutocompleteService.TryResolvePlant(definitions, "aceituno", out var definition);

            Assert.That(resolved, Is.True);
            Assert.That(definition.CommonNameCanonical, Is.EqualTo("olivo"));
        }
    }
}
