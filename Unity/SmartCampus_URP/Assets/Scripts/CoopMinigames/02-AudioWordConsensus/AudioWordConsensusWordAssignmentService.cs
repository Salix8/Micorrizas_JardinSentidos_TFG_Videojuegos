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
            out Dictionary<ulong, string> assignments)
        {
            assignments = new Dictionary<ulong, string>();

            if (receiverClientIds == null || receiverClientIds.Count == 0 || string.IsNullOrWhiteSpace(correctWord))
            {
                return false;
            }

            var requiredDistractorCount = Math.Max(0, receiverClientIds.Count - 1);
            var sanitizedDistractors = SanitizeDistinctWords(distractorWords, correctWord);
            if (sanitizedDistractors.Count < requiredDistractorCount)
            {
                return false;
            }

            var shuffledReceivers = new List<ulong>(receiverClientIds);
            var random = new Random(randomSeed);
            ShuffleInPlace(shuffledReceivers, random);
            ShuffleInPlace(sanitizedDistractors, random);

            assignments[shuffledReceivers[0]] = correctWord.Trim();
            for (var index = 1; index < shuffledReceivers.Count; index++)
            {
                assignments[shuffledReceivers[index]] = sanitizedDistractors[index - 1];
            }

            return true;
        }

        private static List<string> SanitizeDistinctWords(IReadOnlyList<string> sourceWords, string forbiddenWord)
        {
            var results = new List<string>();
            var seenWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            seenWords.Add(forbiddenWord.Trim());

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
