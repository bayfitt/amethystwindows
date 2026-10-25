using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Vanara.PInvoke;
using WindowsDesktop;

using AmethystWindows.Settings;

namespace AmethystWindows.DesktopWindowsManager
{
    partial class DesktopWindowsManager
    {
        public IEnumerable<Rectangle> GridGenerator(
            int mWidth,
            int mHeight,
            int windowsCount,
            int factor,
            Layout layout,
            int layoutPadding,
            IReadOnlyList<int>? paneIndices = null,
            int secondaryFactor = 0,
            int gridBottomRowFactor = 0,
            int gridRightColumnFactor = 0)
        {
            int i = 0;
            int j = 0;
            int horizStep;
            int vertStep;
            int tiles;
            int horizSize;
            int vertSize;
            bool isFirstLine;
            switch (layout)
            {
                case Layout.Horizontal:
                    horizSize = mWidth / windowsCount;
                    j = 0;
                    for (i = 0; i < windowsCount; i++)
                    {
                        int lastPadding = i == (windowsCount - 1) ? 0 : layoutPadding;
                        yield return new Rectangle(i * horizSize, j, horizSize - lastPadding, mHeight);
                    }
                    break;
                case Layout.Vertical:
                    vertSize = mHeight / windowsCount;
                    j = 0;
                    for (i = 0; i < windowsCount; i++)
                    {
                        int lastPadding = i == (windowsCount - 1) ? 0 : layoutPadding;
                        yield return new Rectangle(j, i * vertSize, mWidth, vertSize - lastPadding);
                    }
                    break;
                case Layout.HorizGrid:
                    horizStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                    vertStep = Math.Max(windowsCount / horizStep, 1);
                    tiles = horizStep * vertStep;
                    horizSize = mWidth / horizStep;
                    vertSize = mHeight / vertStep;
                    isFirstLine = true;

                    if (windowsCount != tiles || windowsCount == 3)
                    {
                        if (windowsCount == 3)
                        {
                            vertStep--;
                            vertSize = mHeight / vertStep;
                        }

                        while (windowsCount > 0)
                        {
                            int lastPaddingI = i == (horizStep - 1) ? 0 : layoutPadding;
                            int lastPaddingJ = j == (vertStep - 1) ? 0 : layoutPadding;
                            yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                            i++;
                            if (i >= horizStep)
                            {
                                i = 0;
                                j++;
                            }
                            if (j == vertStep - 1 && isFirstLine)
                            {
                                horizStep++;
                                horizSize = mWidth / horizStep;
                                isFirstLine = false;
                            }
                            windowsCount--;
                        }
                    }
                    else
                    {
                        while (windowsCount > 0)
                        {
                            int lastPaddingI = i == (horizStep - 1) ? 0 : layoutPadding;
                            int lastPaddingJ = j == (vertStep - 1) ? 0 : layoutPadding;
                            yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                            i++;
                            if (i >= horizStep)
                            {
                                i = 0;
                                j++;
                            }
                            windowsCount--;
                        }
                    }
                    break;
                case Layout.VertGrid:
                    vertStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                    horizStep = Math.Max(windowsCount / vertStep, 1);
                    tiles = horizStep * vertStep;
                    vertSize = mHeight / vertStep;
                    horizSize = mWidth / horizStep;
                    isFirstLine = true;

                    if (windowsCount != tiles || windowsCount == 3)
                    {
                        if (windowsCount == 3)
                        {
                            horizStep--;
                            horizSize = mWidth / horizStep;
                        }

                        while (windowsCount > 0)
                        {
                            int lastPaddingI = i == (horizStep - 1) ? 0 : layoutPadding;
                            int lastPaddingJ = j == (vertStep - 1) ? 0 : layoutPadding;
                            yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                            j++;
                            if (j >= vertStep)
                            {
                                j = 0;
                                i++;
                            }
                            if (i == horizStep - 1 && isFirstLine)
                            {
                                vertStep++;
                                vertSize = mHeight / vertStep;
                                isFirstLine = false;
                            }
                            windowsCount--;
                        }
                    }
                    else
                    {
                        while (windowsCount > 0)
                        {
                            int lastPaddingI = i == (horizStep - 1) ? 0 : layoutPadding;
                            int lastPaddingJ = j == (vertStep - 1) ? 0 : layoutPadding;
                            yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                            j++;
                            if (j >= vertStep)
                            {
                                j = 0;
                                i++;
                            }
                            windowsCount--;
                        }
                    }
                    break;
                case Layout.ThreeColumn:
                    foreach (Rectangle rectangle in GenerateSlotLayout(mWidth, mHeight, windowsCount, layoutPadding, 3, false, paneIndices, factor, secondaryFactor, gridBottomRowFactor, gridRightColumnFactor, mainWindowViewModel.Step))
                    {
                        yield return rectangle;
                    }
                    break;
                case Layout.Grid2x2:
                    foreach (Rectangle rectangle in GenerateSlotLayout(mWidth, mHeight, windowsCount, layoutPadding, 4, true, paneIndices, factor, secondaryFactor, gridBottomRowFactor, gridRightColumnFactor, mainWindowViewModel.Step))
                    {
                        yield return rectangle;
                    }
                    break;
                case Layout.Monocle:
                    for (i = 0; i < windowsCount; i++)
                    {
                        yield return new Rectangle(0, 0, mWidth, mHeight);
                    }
                    break;
                case Layout.Wide:
                    if (windowsCount == 1) yield return new Rectangle(0, 0, mWidth, mHeight);
                    else
                    {
                        int size = mWidth / (windowsCount - 1);
                        for (i = 0; i < windowsCount - 1; i++)
                        {
                            int lastPaddingI = windowsCount == 1 ? 0 : layoutPadding;
                            int lastPaddingJ = i == (windowsCount - 2) ? 0 : layoutPadding;

                            if (i == 0) yield return new Rectangle(0, 0, mWidth, mHeight / 2 + factor * mainWindowViewModel.Step - (lastPaddingI /2));
                            yield return new Rectangle(i * size, mHeight / 2 + factor * mainWindowViewModel.Step + (lastPaddingI / 2), size - lastPaddingJ, mHeight / 2 - factor * mainWindowViewModel.Step - (lastPaddingI /2));
                        }
                    }
                    break;
                case Layout.Tall:
                    if (windowsCount == 1 || (paneIndices != null && paneIndices.Take(windowsCount).Distinct().Count() == 1))
                    {
                        for (i = 0; i < windowsCount; i++)
                        {
                            yield return new Rectangle(0, 0, mWidth, mHeight);
                        }
                    }
                    else
                    {
                        int masterWidth = mWidth / 2 + factor * mainWindowViewModel.Step;
                        int stackX = masterWidth + layoutPadding;
                        int stackWidth = mWidth - stackX;
                        Rectangle leftPane = new Rectangle(0, 0, masterWidth, mHeight);
                        Rectangle rightPane = new Rectangle(stackX, 0, stackWidth, mHeight);
                        for (i = 0; i < windowsCount; i++)
                        {
                            int paneIndex = paneIndices != null && i < paneIndices.Count ? paneIndices[i] : (i == 0 ? 0 : 1);
                            yield return paneIndex == 0 ? leftPane : rightPane;
                        }
                    }
                    break;
            }
        }

        private static IEnumerable<Rectangle> GenerateSlotLayout(
            int width,
            int height,
            int windowsCount,
            int layoutPadding,
            int slotCapacity,
            bool twoByTwo,
            IReadOnlyList<int>? paneIndices,
            int factor,
            int secondaryFactor,
            int gridBottomRowFactor,
            int gridRightColumnFactor,
            int step)
        {
            if (windowsCount <= 0)
            {
                yield break;
            }

            int[] assignedSlots = new int[windowsCount];
            for (int i = 0; i < windowsCount; i++)
            {
                int slot = paneIndices != null && i < paneIndices.Count ? paneIndices[i] : i;
                assignedSlots[i] = ((slot % slotCapacity) + slotCapacity) % slotCapacity;
            }

            int[] occupiedSlots = assignedSlots.Distinct().ToArray();
            if (occupiedSlots.Length == 1)
            {
                Rectangle fullWorkArea = new Rectangle(0, 0, width, height);
                for (int i = 0; i < windowsCount; i++)
                {
                    yield return fullWorkArea;
                }
                yield break;
            }

            int columns = twoByTwo ? Math.Min(2, occupiedSlots.Length) : occupiedSlots.Length;
            int rows = twoByTwo && occupiedSlots.Length > 2 ? 2 : 1;
            int[] firstRowColumnSplits = twoByTwo
                ? new[] { GetClampedSplitPosition(width, width / 2, factor, step, columns) }
                : GetThreeColumnDividerPositions(width, columns, factor, secondaryFactor, step);
            int[] secondRowColumnSplits = twoByTwo
                ? new[] { GetClampedSplitPosition(width, width / 2, gridBottomRowFactor, step, columns) }
                : firstRowColumnSplits;
            int[] firstColumnRowSplits = rows > 1
                ? new[] { GetClampedSplitPosition(height, height / 2, secondaryFactor, step, rows) }
                : Array.Empty<int>();
            int[] secondColumnRowSplits = rows > 1
                ? new[] { GetClampedSplitPosition(height, height / 2, gridRightColumnFactor, step, rows) }
                : Array.Empty<int>();
            Dictionary<int, Rectangle> rectanglesBySlot = new Dictionary<int, Rectangle>();

            for (int slotPosition = 0; slotPosition < occupiedSlots.Length; slotPosition++)
            {
                int column = slotPosition % columns;
                int row = slotPosition / columns;
                int[] columnSplits = row == 0 ? firstRowColumnSplits : secondRowColumnSplits;
                int[] rowSplits = column == 0 ? firstColumnRowSplits : secondColumnRowSplits;
                int x = column == 0 ? 0 : columnSplits[column - 1];
                int y = row == 0 ? 0 : rowSplits[row - 1];
                int columnEnd = column == columns - 1 ? width : columnSplits[column];
                int rowEnd = row == rows - 1 ? height : rowSplits[row];
                int tileWidth = Math.Max(0, columnEnd - x - (column == columns - 1 ? 0 : layoutPadding));
                int tileHeight = Math.Max(0, rowEnd - y - (row == rows - 1 ? 0 : layoutPadding));
                rectanglesBySlot[occupiedSlots[slotPosition]] = new Rectangle(x, y, tileWidth, tileHeight);
            }

            for (int i = 0; i < windowsCount; i++)
            {
                yield return rectanglesBySlot[assignedSlots[i]];
            }
        }

        internal static int[] GetThreeColumnDividerPositions(int width, int columns, int factor, int secondaryFactor, int step)
        {
            if (columns <= 1)
            {
                return Array.Empty<int>();
            }

            if (columns == 2)
            {
                return new[] { GetClampedSplitPosition(width, width / 2, factor, step, columns) };
            }

            int minimumPaneWidth = GetMinimumPaneExtent(width, 3);
            int first = ClampSplitPosition(width / 3 + factor * step, minimumPaneWidth, width - (2 * minimumPaneWidth));
            int second = ClampSplitPosition((2 * width) / 3 + secondaryFactor * step, first + minimumPaneWidth, width - minimumPaneWidth);
            return new[] { first, second };
        }

        internal static int GetClampedSplitPosition(int length, int baseline, int factor, int step, int paneCount)
        {
            int minimumPaneExtent = GetMinimumPaneExtent(length, paneCount);
            return ClampSplitPosition(baseline + factor * step, minimumPaneExtent, length - minimumPaneExtent);
        }

        internal static int GetMinimumPaneExtent(int length, int paneCount)
        {
            int minimumExtent = Math.Min(120, Math.Max(1, length / Math.Max(2, paneCount * 2)));
            return Math.Min(minimumExtent, Math.Max(1, length / Math.Max(1, paneCount)));
        }

        private static int ClampSplitPosition(int desired, int minimum, int maximum)
        {
            if (maximum < minimum)
            {
                minimum = maximum = Math.Max(0, (minimum + maximum) / 2);
            }
            return Math.Clamp(desired, minimum, maximum);
        }

    }
}
