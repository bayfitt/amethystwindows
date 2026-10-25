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
        public IEnumerable<Rectangle> GridGenerator(int mWidth, int mHeight, int windowsCount, int factor, Layout layout, int layoutPadding, IReadOnlyList<int>? paneIndices = null)
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
                    foreach (Rectangle rectangle in GenerateSlotLayout(mWidth, mHeight, windowsCount, layoutPadding, 3, false, paneIndices))
                    {
                        yield return rectangle;
                    }
                    break;
                case Layout.Grid2x2:
                    foreach (Rectangle rectangle in GenerateSlotLayout(mWidth, mHeight, windowsCount, layoutPadding, 4, true, paneIndices))
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
            IReadOnlyList<int>? paneIndices)
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
            int columnSize = width / columns;
            int rowSize = height / rows;
            Dictionary<int, Rectangle> rectanglesBySlot = new Dictionary<int, Rectangle>();

            for (int slotPosition = 0; slotPosition < occupiedSlots.Length; slotPosition++)
            {
                int column = slotPosition % columns;
                int row = slotPosition / columns;
                int x = column * columnSize;
                int y = row * rowSize;
                int tileWidth = column == columns - 1 ? width - x : Math.Max(0, columnSize - layoutPadding);
                int tileHeight = row == rows - 1 ? height - y : Math.Max(0, rowSize - layoutPadding);
                rectanglesBySlot[occupiedSlots[slotPosition]] = new Rectangle(x, y, tileWidth, tileHeight);
            }

            for (int i = 0; i < windowsCount; i++)
            {
                yield return rectanglesBySlot[assignedSlots[i]];
            }
        }

    }
}
