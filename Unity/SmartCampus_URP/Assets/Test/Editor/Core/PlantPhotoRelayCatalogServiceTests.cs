using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.PlantPhotoRelay;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class PlantPhotoRelayCatalogServiceTests
    {
        [Test]
        public void TryParse_WithValidCsv_BuildsDefinitions()
        {
            const string csv =
                "commonNameCanonical,displayCommonName,acceptedCommonNameVariants,plantType,surfaceTexture,hasThorns,hasFruit,leafType,sizeCategory\n" +
                "olivo,Olivo,olivera|aceituno,Arbol,rugosa,false,true,simple,mediano\n" +
                "romero,Romero,romeru,Arbusto,fina,false,false,lineal,pequeno\n";

            var parsed = PlantPhotoRelayCatalogService.TryParse(csv, out var definitions, out var errorMessage);

            Assert.That(parsed, Is.True, errorMessage);
            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].CommonNameCanonical, Is.EqualTo("olivo"));
            Assert.That(definitions[0].AcceptedCommonNameVariants, Does.Contain("olivera"));
        }

        [Test]
        public void TryParse_WithMissingColumn_Fails()
        {
            const string csv =
                "commonNameCanonical,displayCommonName,plantType\n" +
                "olivo,Olivo,Arbol\n";

            var parsed = PlantPhotoRelayCatalogService.TryParse(csv, out _, out var errorMessage);

            Assert.That(parsed, Is.False);
            Assert.That(errorMessage, Does.Contain("acceptedCommonNameVariants"));
        }
    }
}
