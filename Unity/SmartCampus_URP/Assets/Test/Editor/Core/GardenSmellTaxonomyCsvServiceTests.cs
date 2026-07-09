using NUnit.Framework;
using System.IO;
using SmartCampus.Coop.Minigames.GardenSmellTaxonomy;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenSmellTaxonomyCsvServiceTests
    {
        private const string ConfiguredCsvRelativePath = "CoopMinigames/05-GardenSmellTaxonomy/GardenSmellTaxonomyPlants.csv";

        [Test]
        public void TryParse_WithCommonNameColumn_StoresCommonName()
        {
            const string csv =
                "plantId,commonName,scientificName,imagePath,correctCategory\n" +
                "lavandula_dentata,Lavanda,Lavandula dentata,Plants/lavandula.png,Decoracion\n" +
                "mentha_spicata,Hierbabuena,Mentha spicata,Plants/mentha.png,Alimentacion\n" +
                "aloe_vera,Aloe vera,Aloe vera,Plants/aloe.png,Curacion\n";

            var parsed = GardenSmellTaxonomyCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions[0].CommonName, Is.EqualTo("Lavanda"));
        }

        [Test]
        public void TryParse_WithoutCommonNameColumn_DerivesFallbackFromPlantId()
        {
            const string csv =
                "plantId,scientificName,imagePath,correctCategory\n" +
                "lavandula_dentata,Lavandula dentata,Plants/lavandula.png,Decoracion\n" +
                "mentha_spicata,Mentha spicata,Plants/mentha.png,Alimentacion\n" +
                "aloe_vera,Aloe vera,Plants/aloe.png,Curacion\n";

            var parsed = GardenSmellTaxonomyCsvService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions[0].CommonName, Is.EqualTo("Lavandula Dentata"));
        }

        [Test]
        public void ConfiguredCsvReferencesExistingImageFiles()
        {
            var streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            var csvPath = Path.Combine(streamingAssetsPath, ConfiguredCsvRelativePath);
            var csvDirectory = Path.GetDirectoryName(csvPath);

            Assert.That(File.Exists(csvPath), Is.True, $"No existe el CSV configurado: {csvPath}");

            var parsed = GardenSmellTaxonomyCsvService.TryParse(
                File.ReadAllText(csvPath),
                out var definitions,
                out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(csvDirectory, Is.Not.Null.And.Not.Empty);

            foreach (var definition in definitions)
            {
                var imagePath = Path.IsPathRooted(definition.ImagePath)
                    ? definition.ImagePath
                    : Path.Combine(csvDirectory, definition.ImagePath);

                Assert.That(
                    File.Exists(imagePath),
                    Is.True,
                    $"La planta '{definition.PlantId}' referencia una imagen que no existe: {definition.ImagePath}");
            }
        }
    }
}
