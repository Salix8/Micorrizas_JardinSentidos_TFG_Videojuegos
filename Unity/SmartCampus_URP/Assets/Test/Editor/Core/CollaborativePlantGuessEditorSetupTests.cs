using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessEditorSetupTests
    {
        [Test]
        public void CollaborativePlantGuessSetup_HasSingleMenuEntry()
        {
            var menuItems = typeof(CollaborativePlantGuessMinigameSetup)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(method => method.GetCustomAttributes<MenuItem>())
                .Where(attribute => attribute.menuItem.Contains("Collaborative Plant Guess"))
                .ToArray();

            Assert.That(menuItems.Length, Is.EqualTo(1));
            Assert.That(menuItems[0].menuItem, Is.EqualTo("Tools/Coop/Setup Collaborative Plant Guess Minigame"));
        }
    }
}
