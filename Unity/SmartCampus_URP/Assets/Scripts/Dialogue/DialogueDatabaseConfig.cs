using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/Dialogue Database", fileName = "DialogueDatabaseConfig")]
    public sealed class DialogueDatabaseConfig : ScriptableObject
    {
        [Header("Source")]
        [SerializeField] private TextAsset csvFile;

        [Header("Localization")]
        [SerializeField] private string defaultLocale = "es-ES";
        [SerializeField] private string fallbackLocale = "en-US";

        private bool hasCache;
        private string cachedErrorMessage = string.Empty;
        private List<DialogueLine> cachedLines = new();
        private Dictionary<string, DialogueLine> cachedLinesById = new(StringComparer.OrdinalIgnoreCase);

        public string DefaultLocale => string.IsNullOrWhiteSpace(defaultLocale) ? "es-ES" : defaultLocale.Trim();
        public string FallbackLocale => string.IsNullOrWhiteSpace(fallbackLocale) ? DefaultLocale : fallbackLocale.Trim();

        public bool TryGetAllLines(out IReadOnlyList<DialogueLine> lines, out string errorMessage)
        {
            EnsureCache();
            lines = cachedLines;
            errorMessage = cachedErrorMessage;
            return string.IsNullOrWhiteSpace(errorMessage);
        }

        public bool TryGetLine(string stringId, out DialogueLine line, out string errorMessage)
        {
            line = null;
            EnsureCache();
            if (!string.IsNullOrWhiteSpace(cachedErrorMessage))
            {
                errorMessage = cachedErrorMessage;
                return false;
            }

            if (string.IsNullOrWhiteSpace(stringId))
            {
                errorMessage = "No se puede buscar un dialogo sin String ID.";
                return false;
            }

            if (!cachedLinesById.TryGetValue(stringId.Trim(), out line))
            {
                errorMessage = $"No existe el dialogo con String ID '{stringId}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool TryGetLinesByIds(IReadOnlyList<string> stringIds, out IReadOnlyList<DialogueLine> lines, out string errorMessage)
        {
            var result = new List<DialogueLine>();
            lines = result;

            if (stringIds == null || stringIds.Count == 0)
            {
                errorMessage = "La secuencia no contiene String IDs.";
                return false;
            }

            for (var index = 0; index < stringIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(stringIds[index]))
                {
                    continue;
                }

                if (!TryGetLine(stringIds[index], out var line, out errorMessage))
                {
                    lines = result;
                    return false;
                }

                result.Add(line);
            }

            if (result.Count == 0)
            {
                errorMessage = "La secuencia no contiene String IDs validos.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool TryGetLinesByActOrLocation(string actOrLocation, out IReadOnlyList<DialogueLine> lines, out string errorMessage)
        {
            var result = new List<DialogueLine>();
            lines = result;

            EnsureCache();
            if (!string.IsNullOrWhiteSpace(cachedErrorMessage))
            {
                errorMessage = cachedErrorMessage;
                return false;
            }

            if (string.IsNullOrWhiteSpace(actOrLocation))
            {
                errorMessage = "No se puede buscar una secuencia sin Act/Location.";
                return false;
            }

            var normalizedActOrLocation = actOrLocation.Trim();
            for (var index = 0; index < cachedLines.Count; index++)
            {
                var line = cachedLines[index];
                if (string.Equals(line.ActOrLocation, normalizedActOrLocation, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(line);
                }
            }

            if (result.Count == 0)
            {
                errorMessage = $"No hay dialogos para Act/Location '{actOrLocation}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public string GetLocalizedText(DialogueLine line, string localeOverride = null)
        {
            if (line == null)
            {
                return string.Empty;
            }

            var locale = string.IsNullOrWhiteSpace(localeOverride) ? DefaultLocale : localeOverride.Trim();
            return line.TryGetText(locale, FallbackLocale, out var text) ? text : string.Empty;
        }

        public void ClearCache()
        {
            hasCache = false;
            cachedErrorMessage = string.Empty;
            cachedLines = new List<DialogueLine>();
            cachedLinesById = new Dictionary<string, DialogueLine>(StringComparer.OrdinalIgnoreCase);
        }

        private void OnValidate()
        {
            ClearCache();
        }

        private void EnsureCache()
        {
            if (hasCache)
            {
                return;
            }

            hasCache = true;
            cachedLines = new List<DialogueLine>();
            cachedLinesById = new Dictionary<string, DialogueLine>(StringComparer.OrdinalIgnoreCase);

            if (csvFile == null)
            {
                cachedErrorMessage = "DialogueDatabaseConfig no tiene asignado ningun CSV.";
                return;
            }

            if (!DialogueCsvService.TryParse(csvFile.text, out cachedLines, out cachedErrorMessage))
            {
                cachedLines.Clear();
                cachedLinesById.Clear();
                return;
            }

            for (var index = 0; index < cachedLines.Count; index++)
            {
                var line = cachedLines[index];
                cachedLinesById[line.StringId] = line;
            }
        }
    }
}
