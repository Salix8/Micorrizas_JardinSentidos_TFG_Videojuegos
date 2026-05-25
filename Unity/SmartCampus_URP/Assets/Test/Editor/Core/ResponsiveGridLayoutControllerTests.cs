using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class ResponsiveGridLayoutControllerTests
    {
        private GameObject root;
        private RectTransform rootRectTransform;
        private GridLayoutGroup gridLayoutGroup;
        private ResponsiveGridLayoutController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "ResponsiveGridRoot",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ResponsiveGridLayoutController));

            rootRectTransform = root.GetComponent<RectTransform>();
            rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 320f);
            rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 920f);

            gridLayoutGroup = root.GetComponent<GridLayoutGroup>();
            gridLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            gridLayoutGroup.spacing = new Vector2(12f, 12f);

            controller = root.GetComponent<ResponsiveGridLayoutController>();

            for (var index = 0; index < 4; index++)
            {
                var child = new GameObject($"Card_{index + 1}", typeof(RectTransform));
                child.transform.SetParent(root.transform, false);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshLayout_WithFourCardsAndMinimumTwoColumns_UsesTwoColumns()
        {
            controller.Configure(
                configuredMinColumns: 2,
                configuredMaxColumns: 2,
                configuredMinCellSize: new Vector2(120f, 170f),
                configuredMaxCellSize: new Vector2(240f, 330f),
                configuredCardAspectRatio: 0.72f);

            controller.RefreshLayout();

            Assert.That(gridLayoutGroup.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(gridLayoutGroup.constraintCount, Is.EqualTo(2));
        }
    }
}
