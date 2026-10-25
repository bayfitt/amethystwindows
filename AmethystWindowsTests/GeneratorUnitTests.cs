using AmethystWindows;
using AmethystWindows.DesktopWindowsManager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Xunit;

namespace AmethystWindowsTests
{
    public class GeneratorUnitTests
    {
        DesktopWindowsManager desktopWindowsManager = new DesktopWindowsManager();
        Layout layout;

        [Fact]
        public void TallUsesOnlyTheAssignedTwoPaneSlots()
        {
            int[] panes = { 0, 1, 1, 0, 1 };
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(1000, 1000, panes.Length, 0, Layout.Tall, 0, panes).ToArray();

            Assert.Equal(new Rectangle(0, 0, 500, 1000), rectangles[0]);
            Assert.Equal(new Rectangle(500, 0, 500, 1000), rectangles[1]);
            Assert.Equal(rectangles[1], rectangles[2]);
            Assert.Equal(rectangles[0], rectangles[3]);
            Assert.Equal(rectangles[1], rectangles[4]);
            Assert.Equal(2, rectangles.Distinct().Count());
        }

        [Fact]
        public void EnteringGridClockwiseResetsEveryDividerToAnEvenSplit()
        {
            ViewModelDesktopMonitor monitor = new ViewModelDesktopMonitor(
                default,
                null!,
                8,
                Layout.ThreeColumn,
                secondaryFactor: -4,
                gridBottomRowFactor: 3,
                gridRightColumnFactor: -2);

            Assert.Equal(Layout.Grid2x2, monitor.RotateLayoutClockwise());
            Assert.Equal(0, monitor.Factor);
            Assert.Equal(0, monitor.SecondaryFactor);
            Assert.Equal(0, monitor.GridBottomRowFactor);
            Assert.Equal(0, monitor.GridRightColumnFactor);
        }

        [Fact]
        public void EnteringGridCounterClockwiseResetsEveryDividerToAnEvenSplit()
        {
            ViewModelDesktopMonitor monitor = new ViewModelDesktopMonitor(
                default,
                null!,
                -5,
                Layout.Monocle,
                secondaryFactor: 6,
                gridBottomRowFactor: -3,
                gridRightColumnFactor: 4);

            Assert.Equal(Layout.Grid2x2, monitor.RotateLayoutCounterClockwise());
            Assert.Equal(0, monitor.Factor);
            Assert.Equal(0, monitor.SecondaryFactor);
            Assert.Equal(0, monitor.GridBottomRowFactor);
            Assert.Equal(0, monitor.GridRightColumnFactor);
        }

        [Fact]
        public void TallFillsTheWorkAreaWhenOneWindowRemainsInTheSecondPane()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1000,
                800,
                1,
                0,
                Layout.Tall,
                0,
                new[] { 1 }).ToArray();

            Assert.Single(rectangles);
            Assert.Equal(new Rectangle(0, 0, 1000, 800), rectangles[0]);
        }

        [Fact]
        public void TallFillsTheWorkAreaWhenOnlyOnePaneHasWindowsRemaining()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1000,
                800,
                3,
                0,
                Layout.Tall,
                0,
                new[] { 1, 1, 1 }).ToArray();

            Assert.Equal(3, rectangles.Length);
            Assert.All(rectangles, rectangle => Assert.Equal(new Rectangle(0, 0, 1000, 800), rectangle));
        }

        [Fact]
        public void ThreeColumnUsesThreeSlotsAndStacksWindowsAfterTheThird()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1200,
                800,
                4,
                0,
                Layout.ThreeColumn,
                0,
                new[] { 0, 1, 2, 0 }).ToArray();

            Assert.Equal(new Rectangle(0, 0, 400, 800), rectangles[0]);
            Assert.Equal(new Rectangle(400, 0, 400, 800), rectangles[1]);
            Assert.Equal(new Rectangle(800, 0, 400, 800), rectangles[2]);
            Assert.Equal(rectangles[0], rectangles[3]);
        }

        [Fact]
        public void TwoByTwoUsesFourSlotsAndStacksWindowsAfterTheFourth()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1200,
                800,
                5,
                0,
                Layout.Grid2x2,
                0,
                new[] { 0, 1, 2, 3, 1 }).ToArray();

            Assert.Equal(new Rectangle(0, 0, 600, 400), rectangles[0]);
            Assert.Equal(new Rectangle(600, 0, 600, 400), rectangles[1]);
            Assert.Equal(new Rectangle(0, 400, 600, 400), rectangles[2]);
            Assert.Equal(new Rectangle(600, 400, 600, 400), rectangles[3]);
            Assert.Equal(rectangles[1], rectangles[4]);
        }

        [Fact]
        public void ThreeColumnLayoutAppliesBothVerticalDividerFactors()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1200,
                800,
                3,
                2,
                Layout.ThreeColumn,
                0,
                new[] { 0, 1, 2 },
                -1).ToArray();

            Assert.Equal(new Rectangle(0, 0, 450, 800), rectangles[0]);
            Assert.Equal(new Rectangle(450, 0, 325, 800), rectangles[1]);
            Assert.Equal(new Rectangle(775, 0, 425, 800), rectangles[2]);
        }

        [Fact]
        public void TwoByTwoLayoutKeepsEachRowAndColumnDividerIndependent()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1200,
                800,
                4,
                2,
                Layout.Grid2x2,
                0,
                new[] { 0, 1, 2, 3 },
                -2,
                -1,
                1).ToArray();

            Assert.Equal(new Rectangle(0, 0, 650, 350), rectangles[0]);
            Assert.Equal(new Rectangle(650, 0, 550, 425), rectangles[1]);
            Assert.Equal(new Rectangle(0, 350, 575, 450), rectangles[2]);
            Assert.Equal(new Rectangle(575, 425, 625, 375), rectangles[3]);
        }

        [Fact]
        public void ThreeColumnExpandsWhenOnlyOneSlotRemainsOccupied()
        {
            Rectangle[] rectangles = desktopWindowsManager.GridGenerator(
                1200,
                800,
                2,
                0,
                Layout.ThreeColumn,
                0,
                new[] { 2, 2 }).ToArray();

            Assert.All(rectangles, rectangle => Assert.Equal(new Rectangle(0, 0, 1200, 800), rectangle));
        }

        [Fact]
        public void LayoutHotkeyCyclesThroughAllFourModesInBothDirections()
        {
            Layout[] expectedClockwise =
            {
                Layout.Tall,
                Layout.ThreeColumn,
                Layout.Grid2x2,
                Layout.Monocle,
                Layout.Tall
            };
            Layout current = Layout.Monocle;

            foreach (Layout expected in expectedClockwise)
            {
                current = ViewModelDesktopMonitor.NextLayout(current, true);
                Assert.Equal(expected, current);
            }

            Layout[] expectedCounterclockwise =
            {
                Layout.Monocle,
                Layout.Grid2x2,
                Layout.ThreeColumn,
                Layout.Tall,
                Layout.Monocle
            };
            current = Layout.Tall;

            foreach (Layout expected in expectedCounterclockwise)
            {
                current = ViewModelDesktopMonitor.NextLayout(current, false);
                Assert.Equal(expected, current);
            }
        }

        [Fact]
        public void ResizingTallMasterPaneUpdatesTheSharedSplit()
        {
            bool adjusted = DesktopWindowsManager.TryCalculateTallSplitFactor(
                new Rectangle(0, 0, 502, 1000),
                new Rectangle(0, 0, 652, 1000),
                0,
                0,
                1000,
                8,
                2,
                4,
                25,
                out int factor);

            Assert.True(adjusted);
            Assert.Equal(6, factor);
        }

        [Fact]
        public void ResizingTallStackPaneMovesTheSameSharedSplit()
        {
            bool adjusted = DesktopWindowsManager.TryCalculateTallSplitFactor(
                new Rectangle(502, 0, 498, 1000),
                new Rectangle(602, 0, 398, 1000),
                1,
                0,
                1000,
                8,
                2,
                4,
                25,
                out int factor);

            Assert.True(adjusted);
            Assert.Equal(4, factor);
        }

        [Fact]
        public void ResizingAnOuterEdgeDoesNotChangeTheTallSplit()
        {
            bool adjusted = DesktopWindowsManager.TryCalculateTallSplitFactor(
                new Rectangle(502, 0, 498, 1000),
                new Rectangle(502, 0, 448, 1000),
                1,
                0,
                1000,
                8,
                2,
                4,
                25,
                out _);

            Assert.False(adjusted);
        }

        [Fact]
        public void ResizingEitherSideOfAThreeColumnDividerChangesItsFactor()
        {
            bool leadingPaneAdjusted = DesktopWindowsManager.TryCalculateSharedSplitFactor(
                new Rectangle(0, 0, 398, 800),
                new Rectangle(0, 0, 498, 800),
                true,
                true,
                0,
                1200,
                8,
                2,
                4,
                25,
                400,
                120,
                680,
                out int leadingFactor);
            bool followingPaneAdjusted = DesktopWindowsManager.TryCalculateSharedSplitFactor(
                new Rectangle(398, 0, 802, 800),
                new Rectangle(498, 0, 702, 800),
                true,
                false,
                0,
                1200,
                8,
                2,
                4,
                25,
                400,
                120,
                680,
                out int followingFactor);

            Assert.True(leadingPaneAdjusted);
            Assert.True(followingPaneAdjusted);
            Assert.Equal(4, leadingFactor);
            Assert.Equal(4, followingFactor);
        }

        [Fact]
        public void ResizingEitherSideOfGridHorizontalDividerChangesItsFactor()
        {
            bool topPaneAdjusted = DesktopWindowsManager.TryCalculateSharedSplitFactor(
                new Rectangle(0, 0, 600, 398),
                new Rectangle(0, 0, 600, 448),
                false,
                true,
                0,
                800,
                8,
                2,
                4,
                25,
                400,
                120,
                680,
                out int topFactor);
            bool bottomPaneAdjusted = DesktopWindowsManager.TryCalculateSharedSplitFactor(
                new Rectangle(0, 402, 600, 398),
                new Rectangle(0, 452, 600, 348),
                false,
                false,
                0,
                800,
                8,
                2,
                4,
                25,
                400,
                120,
                680,
                out int bottomFactor);

            Assert.True(topPaneAdjusted);
            Assert.True(bottomPaneAdjusted);
            Assert.Equal(2, topFactor);
            Assert.Equal(2, bottomFactor);
        }

        [Fact]
        public void ResizingGridOuterEdgeDoesNotChangeEitherSplitFactor()
        {
            bool adjusted = DesktopWindowsManager.TryCalculateSharedSplitFactor(
                new Rectangle(0, 0, 600, 400),
                new Rectangle(-50, 0, 650, 400),
                true,
                true,
                0,
                1200,
                8,
                2,
                4,
                25,
                600,
                120,
                1080,
                out _);

            Assert.False(adjusted);
        }

        [Fact]
        public void GridGeneratorCountOne()
        {
            layout = Layout.Horizontal;
            IEnumerable<Rectangle> gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
            layout = Layout.Vertical;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
            layout = Layout.HorizGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
            layout = Layout.VertGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
            layout = Layout.Monocle;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
            layout = Layout.Tall;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
            layout = Layout.Wide;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 1, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[1]);
        }

        [Fact]
        public void GridGeneratorCountTwo()
        {
            layout = Layout.Horizontal;
            IEnumerable<Rectangle> gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
            layout = Layout.Vertical;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 1000, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
            layout = Layout.VertGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
            layout = Layout.HorizGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 1000, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
            layout = Layout.Monocle;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
            layout = Layout.Wide;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 1000, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
            layout = Layout.Tall;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 2, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[2]);
        }

        [Fact]
        public void GridGeneratorCountThree()
        {
            layout = Layout.Horizontal;
            IEnumerable<Rectangle> gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 333, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(333, 0, 333, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(666, 0, 333, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
            layout = Layout.Vertical;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 333, 1000, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 666, 1000, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
            layout = Layout.VertGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
            layout = Layout.HorizGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
            layout = Layout.Monocle;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
            layout = Layout.Wide;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
            layout = Layout.Tall;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 3, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
        }

        [Fact]
        public void GridGeneratorCountFour()
        {
            layout = Layout.Horizontal;
            IEnumerable<Rectangle> gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 250, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(250, 0, 250, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 250, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(750, 0, 250, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
            layout = Layout.Vertical;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 250, 1000, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 500, 1000, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(0, 750, 1000, 250));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
            layout = Layout.VertGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
            layout = Layout.HorizGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
            layout = Layout.Monocle;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
            layout = Layout.Wide;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(333, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(666, 500, 333, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
            layout = Layout.Tall;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 4, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 0, 500, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
        }

        [Fact]
        public void GridGeneratorCountFive()
        {
            layout = Layout.Horizontal;
            IEnumerable<Rectangle> gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 5, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(200, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(400, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(600, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(800, 0, 200, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
            layout = Layout.Vertical;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 5, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 200, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 400, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(0, 600, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(0, 800, 1000, 200));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
            layout = Layout.VertGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 5, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(500, 666, 500, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
            layout = Layout.HorizGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 5, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(333, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(666, 500, 333, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
            layout = Layout.Wide;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 5, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 250, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(250, 500, 250, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 250, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(750, 500, 250, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
            layout = Layout.Tall;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 5, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(500, 0, 500, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
        }

        [Fact]
        public void GridGeneratorCountSix()
        {
            layout = Layout.VertGrid;
            IEnumerable<Rectangle> gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 6, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(333, 0, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(333, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(666, 0, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[5], new Rectangle(666, 500, 333, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[6]);
            layout = Layout.HorizGrid;
            gridGenerator = desktopWindowsManager.GridGenerator(1000, 1000, 6, 0, layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(0, 666, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[5], new Rectangle(500, 666, 500, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[6]);
        }
    }
}
