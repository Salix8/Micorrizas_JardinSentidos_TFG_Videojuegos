using System.Text;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public static class PlantPhotoRelayPromptService
    {
        public static string BuildPrompt(PlantPhotoRelayPlantDefinition plantDefinition)
        {
            if (plantDefinition == null)
            {
                return "Busca una planta del catalogo.";
            }

            var builder = new StringBuilder();
            builder.Append(plantDefinition.PlantType);
            builder.Append(' ');
            builder.Append(plantDefinition.SizeCategory.ToLowerInvariant());
            builder.Append(" con superficie ");
            builder.Append(plantDefinition.SurfaceTexture.ToLowerInvariant());
            builder.Append(", hoja ");
            builder.Append(plantDefinition.LeafType.ToLowerInvariant());
            builder.Append(plantDefinition.HasFruit ? " y con fruto" : " y sin fruto");
            builder.Append(plantDefinition.HasThorns ? ", con pinchos." : ", sin pinchos.");
            return builder.ToString();
        }
    }
}
