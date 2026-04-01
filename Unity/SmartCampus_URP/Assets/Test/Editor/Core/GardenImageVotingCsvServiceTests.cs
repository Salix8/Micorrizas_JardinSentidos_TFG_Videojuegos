using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenImageVoting;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenImageVotingCsvServiceTests
    {
        [Test]
        public void Parse_ValidTemplate_ReturnsOrderedDefinitions()
        {
            var csv = string.Join("\n", new[]
            {
                "roundIndex,deviceSlot,topic,title,imagePath,isSeenInGarden",
                "2,1,Cipreses,Tronco,folder/tronco.png,false",
                "1,1,Cipreses,Hojas,folder/hojas.png,true",
                "1,2,Cipreses,Copa,folder/copa.png,false"
            });

            var success = GardenImageVotingCsvService.TryParse(
                csv,
                maxSupportedDevices: 6,
                cardsPerDevice: 5,
                allowRepeatedImagesAcrossDevices: true,
                out var definitions,
                out var errorMessage);

            Assert.That(success, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(3));
            Assert.That(definitions[0].RoundIndex, Is.EqualTo(1));
            Assert.That(definitions[0].DeviceSlot, Is.EqualTo(1));
            Assert.That(definitions[0].Title, Is.EqualTo("Hojas"));
            Assert.That(definitions[2].RoundIndex, Is.EqualTo(2));
        }

        [Test]
        public void Parse_DuplicateImageInSameRound_WhenDuplicatesAreDisabled_Fails()
        {
            var csv = string.Join("\n", new[]
            {
                "roundIndex,deviceSlot,topic,title,imagePath,isSeenInGarden",
                "1,1,Cipreses,Hojas,shared.png,false",
                "1,2,Cipreses,Tronco,shared.png,false"
            });

            var success = GardenImageVotingCsvService.TryParse(
                csv,
                maxSupportedDevices: 6,
                cardsPerDevice: 5,
                allowRepeatedImagesAcrossDevices: false,
                out List<GardenImageVotingCardDefinition> _,
                out var errorMessage);

            Assert.That(success, Is.False);
            Assert.That(errorMessage, Does.Contain("repetida"));
        }
    }
}
