using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CoopMinigameThemeConfigTests
    {
        [Test]
        public void DefaultTheme_ExposesMicorrhizalUiBaseline()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();

            Assert.That(theme.ProjectTitle, Is.EqualTo("JARDIN MICORRIZAL"));
            Assert.That(theme.GlobalProgressLabel, Is.EqualTo("RED DE MICORRIZAS"));
            Assert.That(theme.TeamTitle, Is.EqualTo("EQUIPO"));
            Assert.That(theme.Palette.PrimaryGreen.a, Is.EqualTo(1f));
            Assert.That(theme.TopPanelStyle.TitleBandHeight, Is.GreaterThanOrEqualTo(40f));
            Assert.That(theme.BottomPanelStyle.PenaltyLabel, Is.EqualTo("PENALIZACION"));

            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ResolveTeamDisplayName_UsesTeamNameBeforeRoomCode()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();

            var displayName = theme.ResolveTeamDisplayName(" Micorrizas Norte ", "ABCD");

            Assert.That(displayName, Is.EqualTo("Micorrizas Norte"));
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ResolveTeamDisplayName_FallsBackToRoomCodeWhenTeamHasNoName()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();

            var displayName = theme.ResolveTeamDisplayName(" ", "ABCD");

            Assert.That(displayName, Is.EqualTo("SALA ABCD"));
            Object.DestroyImmediate(theme);
        }
    }
}
