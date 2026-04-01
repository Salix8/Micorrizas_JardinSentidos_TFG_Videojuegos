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

        [Test]
        public void Parse_QuotedValuesAndLocalizedBoolean_ReturnsExpectedCardData()
        {
            var csv = string.Join("\n", new[]
            {
                "roundIndex,deviceSlot,topic,title,imagePath,isSeenInGarden",
                "1,2,\"Zona, norte\",\"Flor principal\",\"imagenes/flor,01.png\",si",
                "1,1,Arbustos,Rama,imagenes/rama.png,no"
            });

            var success = GardenImageVotingCsvService.TryParse(
                csv,
                maxSupportedDevices: 4,
                cardsPerDevice: 3,
                allowRepeatedImagesAcrossDevices: true,
                out var definitions,
                out var errorMessage);

            Assert.That(success, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions[1].Topic, Is.EqualTo("Zona, norte"));
            Assert.That(definitions[1].ImagePath, Is.EqualTo("imagenes/flor,01.png"));
            Assert.That(definitions[1].IsSeenInGarden, Is.True);
        }

        [Test]
        public void Parse_MissingRequiredColumn_ReturnsFalse()
        {
            const string csv =
                "roundIndex,deviceSlot,topic,title,imagePath\n" +
                "1,1,Cipreses,Hojas,folder/hojas.png\n";

            var success = GardenImageVotingCsvService.TryParse(
                csv,
                maxSupportedDevices: 6,
                cardsPerDevice: 5,
                allowRepeatedImagesAcrossDevices: true,
                out List<GardenImageVotingCardDefinition> _,
                out var errorMessage);

            Assert.That(success, Is.False);
            Assert.That(errorMessage, Does.Contain("isSeenInGarden"));
        }
    }
}
