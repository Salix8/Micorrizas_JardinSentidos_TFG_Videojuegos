using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenSmellTaxonomy;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenSmellTaxonomySequenceServiceTests
    {
        [Test]
        public void BuildSchedule_RespectsMaximumPlantCount()
        {
            var definitions = new[]
            {
                new GardenSmellTaxonomyPlantDefinition("a", "A", "a.png", GardenSmellTaxonomyCategory.Decoration),
                new GardenSmellTaxonomyPlantDefinition("b", "B", "b.png", GardenSmellTaxonomyCategory.Decoration),
                new GardenSmellTaxonomyPlantDefinition("c", "C", "c.png", GardenSmellTaxonomyCategory.Food),
                new GardenSmellTaxonomyPlantDefinition("d", "D", "d.png", GardenSmellTaxonomyCategory.Healing)
            };

            var schedule = GardenSmellTaxonomySequenceService.BuildSchedule(definitions, 3, shufflePlants: false, seed: 42);

            Assert.That(schedule.Count, Is.EqualTo(3));
            Assert.That(schedule[0].PlantId, Is.EqualTo("a"));
            Assert.That(schedule[2].PlantId, Is.EqualTo("c"));
        }
    }
}
