using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class PlantPhotoRelayEditorSetupTests
    {
        [Test]
        public void PlantPhotoRelaySetup_HasSingleMenuEntry()
        {
            var menuItems = typeof(PlantPhotoRelayMinigameSetup)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(method => method.GetCustomAttributes<MenuItem>())
                .Where(attribute => attribute.menuItem.Contains("Plant Photo Relay"))
                .ToArray();

            Assert.That(menuItems.Length, Is.EqualTo(1));
            Assert.That(menuItems[0].menuItem, Is.EqualTo("Tools/Coop/Setup Plant Photo Relay Minigame"));
        }
    }
}
