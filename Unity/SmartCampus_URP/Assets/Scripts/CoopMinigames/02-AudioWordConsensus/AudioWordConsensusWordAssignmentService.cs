using System;
using System.Collections.Generic;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    public static class AudioWordConsensusWordAssignmentService
    {
        public static bool TryBuildAssignments(
            IReadOnlyList<ulong> receiverClientIds,
            string correctWord,
            IReadOnlyList<string> distractorWords,
            int randomSeed,
            out Dictionary<ulong, List<string>> assignments)
        {
            assignments = new Dictionary<ulong, List<string>>();

            if (receiverClientIds == null || receiverClientIds.Count == 0 || string.IsNullOrWhiteSpace(correctWord))
            {
                return false;
            }

            var trimmedCorrectWord = correctWord.Trim();
            var sanitizedDistractors = SanitizeDistinctWords(distractorWords, trimmedCorrectWord);
            if (receiverClientIds.Count >= 2 && sanitizedDistractors.Count == 0)
            {
                return false;
            }

            var optionWords = new List<string>(sanitizedDistractors.Count + 1)
            {
                trimmedCorrectWord
            };

            optionWords.AddRange(sanitizedDistractors);

            var random = new Random(randomSeed);
            ShuffleInPlace(optionWords, random);

            for (var receiverIndex = 0; receiverIndex < receiverClientIds.Count; receiverIndex++)
            {
                assignments[receiverClientIds[receiverIndex]] = new List<string>(optionWords);
            }

            return true;
        }

        private static List<string> SanitizeDistinctWords(IReadOnlyList<string> sourceWords, string forbiddenWord)
        {
            var results = new List<string>();
            var seenWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                forbiddenWord
            };

            if (sourceWords == null)
            {
                return results;
            }

            for (var index = 0; index < sourceWords.Count; index++)
            {
                var candidate = sourceWords[index]?.Trim();
                if (string.IsNullOrWhiteSpace(candidate) || !seenWords.Add(candidate))
                {
                    continue;
                }

                results.Add(candidate);
            }

            return results;
        }

        private static void ShuffleInPlace<T>(IList<T> values, Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }
    }
}
