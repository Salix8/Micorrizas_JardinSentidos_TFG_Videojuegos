using System.Globalization;
using System.Text;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    public enum GardenSmellTaxonomyCategory : byte
    {
        Decoration = 0,
        Food = 1,
        Healing = 2
    }

    public static class GardenSmellTaxonomyCategoryLabels
    {
        public static string GetDisplayName(GardenSmellTaxonomyCategory category)
        {
            switch (category)
            {
                case GardenSmellTaxonomyCategory.Decoration:
                    return "Decoracion";
                case GardenSmellTaxonomyCategory.Food:
                    return "Alimentacion";
                case GardenSmellTaxonomyCategory.Healing:
                    return "Curacion";
                default:
                    return "Sin categoria";
            }
        }

        public static string GetSupportText(GardenSmellTaxonomyCategory category)
        {
            switch (category)
            {
                case GardenSmellTaxonomyCategory.Decoration:
                    return "Uso ornamental";
                case GardenSmellTaxonomyCategory.Food:
                    return "Uso culinario";
                case GardenSmellTaxonomyCategory.Healing:
                    return "Uso medicinal";
                default:
                    return string.Empty;
            }
        }

        public static string GetBadgeLabel(GardenSmellTaxonomyCategory category)
        {
            switch (category)
            {
                case GardenSmellTaxonomyCategory.Decoration:
                    return "D";
                case GardenSmellTaxonomyCategory.Food:
                    return "A";
                case GardenSmellTaxonomyCategory.Healing:
                    return "C";
                default:
                    return "?";
            }
        }

        public static bool TryParse(string rawValue, out GardenSmellTaxonomyCategory category)
        {
            switch (Normalize(rawValue))
            {
                case "decoracion":
                case "ornamental":
                    category = GardenSmellTaxonomyCategory.Decoration;
                    return true;
                case "alimentacion":
                case "culinaria":
                case "comestible":
                    category = GardenSmellTaxonomyCategory.Food;
                    return true;
                case "curacion":
                case "medicinal":
                case "terapeutica":
                case "terapeutico":
                    category = GardenSmellTaxonomyCategory.Healing;
                    return true;
                default:
                    category = GardenSmellTaxonomyCategory.Decoration;
                    return false;
            }
        }

        private static string Normalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            var normalized = rawValue.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            for (var index = 0; index < normalized.Length; index++)
            {
                var currentChar = normalized[index];
                if (CharUnicodeInfo.GetUnicodeCategory(currentChar) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(currentChar))
                {
                    builder.Append(currentChar);
                }
            }

            return builder.ToString();
        }
    }
}
