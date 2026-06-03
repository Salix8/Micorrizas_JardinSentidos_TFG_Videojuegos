namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public static class PlantPhotoRelayCommonNameResolverService
    {
        public static bool TryResolveCanonicalCommonName(
            System.Collections.Generic.IReadOnlyList<PlantPhotoRelayPlantDefinition> plantDefinitions,
            string rawInput,
            out string canonicalCommonName)
        {
            canonicalCommonName = string.Empty;
            if (!PlantPhotoRelayAutocompleteService.TryResolvePlant(plantDefinitions, rawInput, out var plantDefinition))
            {
                return false;
            }

            canonicalCommonName = plantDefinition.CommonNameCanonical;
            return true;
        }
    }
}
