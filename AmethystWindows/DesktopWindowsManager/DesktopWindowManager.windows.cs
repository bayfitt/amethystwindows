using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.DesktopWindowsManager
{
    partial class DesktopWindowsManager
    {
        public void AddWindow(DesktopWindow desktopWindow, bool makeActive = true)
        {
            Pair<string, string> configurableFilter = mainWindowViewModel.ConfigurableFilters.FirstOrDefault(f => f.Key == desktopWindow.AppName);
            Pair<VirtualDesktop, HMONITOR> key = desktopWindow.GetDesktopMonitor();

            if (!Windows.ContainsKey(key) && !WindowsSubscribed.ContainsKey(key))
            {
                WindowsSubscribed.Add(key, false);
                Windows.Add(key, new ObservableCollection<DesktopWindow>());
                SubscribeWindowsCollectionChanged(key, true);
            }

            if (FixedFilters.All(s => !desktopWindow.AppName.StartsWith(s)) 
                && desktopWindow.AppName != String.Empty &&
                !Windows[key].Contains(desktopWindow))
            { 
                if (configurableFilter.Equals(null))
                {
                    AddWindowToPane(desktopWindow, null, makeActive);
                }
                else
                {
                    if (configurableFilter.Value != "*" && configurableFilter.Value != desktopWindow.ClassName)
                    {
                        AddWindowToPane(desktopWindow, null, makeActive);
                    }
                }
            }
            else if (Windows.ContainsKey(key) && Windows[key].Contains(desktopWindow) && makeActive)
            {
                ActivePaneIndices[key] = desktopWindow.PaneIndex;
            }
        }

        private void AddWindowToPane(DesktopWindow desktopWindow, int? requestedPane, bool makeActive)
        {
            Pair<VirtualDesktop, HMONITOR> key = desktopWindow.GetDesktopMonitor();
            if (!Windows.ContainsKey(key))
            {
                Windows[key] = new ObservableCollection<DesktopWindow>();
                WindowsSubscribed[key] = false;
                SubscribeWindowsCollectionChanged(key, true);
            }
            ObservableCollection<DesktopWindow> windows = Windows[key];
            int paneCount = GetPaneCount(key);
            int activePane = ActivePaneIndices.TryGetValue(key, out int currentPane)
                ? ((currentPane % paneCount) + paneCount) % paneCount
                : 0;
            int pane = requestedPane.HasValue
                ? ((requestedPane.Value % paneCount) + paneCount) % paneCount
                : GetNextPane(windows, activePane, paneCount);

            foreach (DesktopWindow existing in windows.Where(window => window.PaneIndex == pane))
            {
                existing.StackOrder++;
            }

            desktopWindow.PaneIndex = pane;
            desktopWindow.StackOrder = 0;
            windows.Add(desktopWindow);
            if (makeActive || windows.Count == 1)
            {
                ActivePaneIndices[key] = pane;
            }
        }

        private int GetPaneCount(Pair<VirtualDesktop, HMONITOR> key)
        {
            ViewModelDesktopMonitor monitor = mainWindowViewModel.DesktopMonitors.FirstOrDefault(item =>
                item.Monitor.Equals(key.Value) && item.VirtualDesktop.Equals(key.Key));
            return monitor?.Layout.PaneCount() ?? Layout.Tall.PaneCount();
        }

        private static int GetNextPane(IReadOnlyCollection<DesktopWindow> windows, int activePane, int paneCount)
        {
            if (windows.Count == 0)
            {
                return 0;
            }

            for (int offset = 1; offset <= paneCount; offset++)
            {
                int candidate = (activePane + offset) % paneCount;
                if (!windows.Any(window => window.PaneIndex == candidate))
                {
                    return candidate;
                }
            }

            // Every slot is occupied, so a new window covers the next inactive slot.
            return (activePane + 1) % paneCount;
        }

        public void ReassignWindowsToLayout(Pair<VirtualDesktop, HMONITOR> key, Layout layout)
        {
            if (!Windows.TryGetValue(key, out ObservableCollection<DesktopWindow> windows) || windows.Count == 0)
            {
                return;
            }

            int paneCount = layout.PaneCount();
            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].PaneIndex = i % paneCount;
            }

            foreach (IGrouping<int, DesktopWindow> pane in windows.GroupBy(window => window.PaneIndex))
            {
                int stackOrder = pane.Count() - 1;
                foreach (DesktopWindow window in pane)
                {
                    window.StackOrder = stackOrder--;
                }
            }

            DesktopWindow foreground = windows.FirstOrDefault(window => window.Window == User32.GetForegroundWindow());
            ActivePaneIndices[key] = foreground?.PaneIndex ?? windows[windows.Count - 1].PaneIndex;
            Draw(key);
        }

        public void ActivateWindow(DesktopWindow desktopWindow)
        {
            DesktopWindow existing = FindWindow(desktopWindow.Window);
            if (existing != null)
            {
                ActivePaneIndices[existing.GetDesktopMonitor()] = existing.PaneIndex;
            }
            else
            {
                AddWindow(desktopWindow);
            }
        }

        public void RemoveWindow(DesktopWindow desktopWindow)
        {
            Pair<VirtualDesktop, HMONITOR> key = desktopWindow.GetDesktopMonitor();
            if (!Windows.ContainsKey(key)) return;

            ObservableCollection<DesktopWindow> windows = Windows[key];
            if (!windows.Remove(desktopWindow)) return;

            if (windows.Count == 0)
            {
                ActivePaneIndices.Remove(key);
                return;
            }

            int[] occupiedPanes = windows.Select(window => window.PaneIndex).Distinct().ToArray();
            if (occupiedPanes.Length == 1)
            {
                // If one pane's entire stack is gone, the remaining stack fills the work area.
                // Normalize its slot so the next window opens in the opposite pane.
                foreach (DesktopWindow remainingWindow in windows)
                {
                    remainingWindow.PaneIndex = 0;
                }
                ActivePaneIndices[key] = 0;
            }
            else if (!occupiedPanes.Contains(ActivePaneIndices.GetValueOrDefault(key)))
            {
                DesktopWindow foreground = windows.FirstOrDefault(window => window.Window == User32.GetForegroundWindow());
                ActivePaneIndices[key] = foreground?.PaneIndex ?? windows.FirstOrDefault()?.PaneIndex ?? 0;
            }

            // Window destruction/hide events must immediately reflow the remaining pane.
            Draw(key);
        }

        public void RepositionWindow(DesktopWindow oldDesktopWindow, DesktopWindow newDesktopWindow)
        {
            int pane = oldDesktopWindow.PaneIndex;
            int stackOrder = oldDesktopWindow.StackOrder;
            RemoveWindow(oldDesktopWindow);
            AddWindowToPane(newDesktopWindow, pane, newDesktopWindow.Window == User32.GetForegroundWindow());
            newDesktopWindow.StackOrder = stackOrder;
        }

        public bool TryAdjustSharedSplitFromResize(DesktopWindow oldDesktopWindow, DesktopWindow resizedDesktopWindow)
        {
            Pair<VirtualDesktop, HMONITOR> desktopMonitor = oldDesktopWindow.GetDesktopMonitor();
            if (!desktopMonitor.Equals(resizedDesktopWindow.GetDesktopMonitor()) ||
                !Windows.TryGetValue(desktopMonitor, out ObservableCollection<DesktopWindow> windows))
            {
                return false;
            }

            ViewModelDesktopMonitor monitor = mainWindowViewModel.DesktopMonitors.FirstOrDefault(item =>
                item.Monitor.Equals(desktopMonitor.Value) && item.VirtualDesktop.Equals(desktopMonitor.Key));
            if (monitor == null)
            {
                return false;
            }

            if (monitor.Layout == Layout.Tall)
            {
                return TryAdjustTallSplitFromResize(oldDesktopWindow, resizedDesktopWindow);
            }

            if (monitor.Layout != Layout.ThreeColumn && monitor.Layout != Layout.Grid2x2)
            {
                return false;
            }

            int[] occupiedPanes = windows.Select(window => window.PaneIndex).Distinct().OrderBy(pane => pane).ToArray();
            int panePosition = Array.IndexOf(occupiedPanes, oldDesktopWindow.PaneIndex);
            if (occupiedPanes.Length < 2 || panePosition < 0)
            {
                return false;
            }

            User32.MONITORINFO monitorInfo = new User32.MONITORINFO();
            monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);
            if (!User32.GetMonitorInfo(desktopMonitor.Value, ref monitorInfo))
            {
                return false;
            }

            int layoutX = monitorInfo.rcWork.X + mainWindowViewModel.MarginLeft;
            int layoutY = monitorInfo.rcWork.Y + mainWindowViewModel.MarginTop;
            int layoutWidth = monitorInfo.rcWork.Width - mainWindowViewModel.MarginLeft - mainWindowViewModel.MarginRight;
            int layoutHeight = monitorInfo.rcWork.Height - mainWindowViewModel.MarginTop - mainWindowViewModel.MarginBottom;
            int columns = monitor.Layout == Layout.ThreeColumn ? occupiedPanes.Length : Math.Min(2, occupiedPanes.Length);
            int column = panePosition % columns;
            Rectangle oldBounds = ToRectangle(oldDesktopWindow.Info.rcWindow);
            Rectangle resizedBounds = ToRectangle(resizedDesktopWindow.Info.rcWindow);
            bool adjusted = false;

            if (monitor.Layout == Layout.ThreeColumn)
            {
                int minimumPaneWidth = GetMinimumPaneExtent(layoutWidth, columns);
                int[] dividers = GetThreeColumnDividerPositions(
                    layoutWidth,
                    columns,
                    monitor.Factor,
                    monitor.SecondaryFactor,
                    mainWindowViewModel.Step);

                if (column > 0)
                {
                    int dividerIndex = column - 1;
                    int previousDivider = dividerIndex == 0 ? 0 : dividers[dividerIndex - 1];
                    int nextDivider = dividerIndex + 1 < dividers.Length ? dividers[dividerIndex + 1] : layoutWidth;
                    int minimumSplit = previousDivider + minimumPaneWidth;
                    int maximumSplit = nextDivider - minimumPaneWidth;
                    int baseline = columns == 2 ? layoutWidth / 2 : (dividerIndex + 1) * layoutWidth / 3;
                    if (TryCalculateSharedSplitFactor(
                        oldBounds, resizedBounds, true, false, layoutX, layoutWidth, resizedDesktopWindow.BorderX,
                        mainWindowViewModel.Padding, mainWindowViewModel.LayoutPadding, mainWindowViewModel.Step,
                        baseline, minimumSplit, maximumSplit, out int factor))
                    {
                        if (dividerIndex == 0) monitor.Factor = factor;
                        else monitor.SecondaryFactor = factor;
                        adjusted = true;
                    }
                }

                if (column < columns - 1)
                {
                    int dividerIndex = column;
                    int previousDivider = dividerIndex == 0 ? 0 : dividers[dividerIndex - 1];
                    int nextDivider = dividerIndex + 1 < dividers.Length ? dividers[dividerIndex + 1] : layoutWidth;
                    int minimumSplit = previousDivider + minimumPaneWidth;
                    int maximumSplit = nextDivider - minimumPaneWidth;
                    int baseline = columns == 2 ? layoutWidth / 2 : (dividerIndex + 1) * layoutWidth / 3;
                    if (TryCalculateSharedSplitFactor(
                        oldBounds, resizedBounds, true, true, layoutX, layoutWidth, resizedDesktopWindow.BorderX,
                        mainWindowViewModel.Padding, mainWindowViewModel.LayoutPadding, mainWindowViewModel.Step,
                        baseline, minimumSplit, maximumSplit, out int factor))
                    {
                        if (dividerIndex == 0) monitor.Factor = factor;
                        else monitor.SecondaryFactor = factor;
                        adjusted = true;
                    }
                }
            }
            else
            {
                adjusted = TryAdjustGridSplitFactorsFromResize(
                    panePosition,
                    occupiedPanes.Length,
                    oldBounds,
                    resizedBounds,
                    layoutX,
                    layoutY,
                    layoutWidth,
                    layoutHeight,
                    resizedDesktopWindow.BorderX,
                    resizedDesktopWindow.BorderY,
                    mainWindowViewModel.Padding,
                    mainWindowViewModel.LayoutPadding,
                    mainWindowViewModel.Step,
                    monitor.Factor,
                    monitor.SecondaryFactor,
                    monitor.GridBottomRowFactor,
                    monitor.GridRightColumnFactor,
                    out int factor,
                    out int secondaryFactor,
                    out int gridBottomRowFactor,
                    out int gridRightColumnFactor);

                if (adjusted)
                {
                    monitor.Factor = factor;
                    monitor.SecondaryFactor = secondaryFactor;
                    monitor.GridBottomRowFactor = gridBottomRowFactor;
                    monitor.GridRightColumnFactor = gridRightColumnFactor;
                }
            }

            return adjusted;
        }

        internal static bool TryAdjustGridSplitFactorsFromResize(
            int panePosition,
            int occupiedPaneCount,
            Rectangle oldBounds,
            Rectangle resizedBounds,
            int layoutX,
            int layoutY,
            int layoutWidth,
            int layoutHeight,
            int borderX,
            int borderY,
            int padding,
            int layoutPadding,
            int step,
            int factor,
            int secondaryFactor,
            int gridBottomRowFactor,
            int gridRightColumnFactor,
            out int adjustedFactor,
            out int adjustedSecondaryFactor,
            out int adjustedGridBottomRowFactor,
            out int adjustedGridRightColumnFactor)
        {
            adjustedFactor = factor;
            adjustedSecondaryFactor = secondaryFactor;
            adjustedGridBottomRowFactor = gridBottomRowFactor;
            adjustedGridRightColumnFactor = gridRightColumnFactor;

            if (occupiedPaneCount < 2 || panePosition < 0 || panePosition >= occupiedPaneCount)
            {
                return false;
            }

            int columns = Math.Min(2, occupiedPaneCount);
            int rows = occupiedPaneCount > 2 ? 2 : 1;
            int column = panePosition % columns;
            int row = panePosition / columns;
            bool hasLeftNeighbor = column > 0;
            bool hasRightNeighbor = column < columns - 1 && panePosition + 1 < occupiedPaneCount && (panePosition + 1) / columns == row;
            bool hasTopNeighbor = row > 0;
            bool hasBottomNeighbor = row < rows - 1 && panePosition + columns < occupiedPaneCount;
            bool adjusted = false;

            if (hasLeftNeighbor || hasRightNeighbor)
            {
                int minimumPaneWidth = GetMinimumPaneExtent(layoutWidth, columns);
                if (TryCalculateSharedSplitFactor(
                    oldBounds, resizedBounds, true, hasRightNeighbor, layoutX, layoutWidth, borderX,
                    padding, layoutPadding, step,
                    layoutWidth / 2, minimumPaneWidth, layoutWidth - minimumPaneWidth, out int splitFactor))
                {
                    if (row == 0)
                    {
                        adjustedFactor = splitFactor;
                    }
                    else
                    {
                        adjustedGridBottomRowFactor = splitFactor;
                    }
                    adjusted = true;
                }
            }

            if (hasTopNeighbor || hasBottomNeighbor)
            {
                int minimumPaneHeight = GetMinimumPaneExtent(layoutHeight, rows);
                if (TryCalculateSharedSplitFactor(
                    oldBounds, resizedBounds, false, hasBottomNeighbor, layoutY, layoutHeight, borderY,
                    padding, layoutPadding, step,
                    layoutHeight / 2, minimumPaneHeight, layoutHeight - minimumPaneHeight, out int splitFactor))
                {
                    if (column == 0)
                    {
                        adjustedSecondaryFactor = splitFactor;
                    }
                    else
                    {
                        adjustedGridRightColumnFactor = splitFactor;
                    }
                    adjusted = true;
                }
            }

            return adjusted;
        }

        internal static bool TryCalculateSharedSplitFactor(
            Rectangle oldBounds,
            Rectangle resizedBounds,
            bool verticalDivider,
            bool leadingPane,
            int layoutStart,
            int layoutLength,
            int borderSize,
            int padding,
            int layoutPadding,
            int step,
            int baseline,
            int minimumSplit,
            int maximumSplit,
            out int factor)
        {
            factor = 0;
            if (layoutLength < 3 || step <= 0 || maximumSplit < minimumSplit)
            {
                return false;
            }

            const int edgeTolerance = 2;
            int oldLeadingEdge = verticalDivider ? oldBounds.Left : oldBounds.Top;
            int newLeadingEdge = verticalDivider ? resizedBounds.Left : resizedBounds.Top;
            int oldTrailingEdge = verticalDivider ? oldBounds.Right : oldBounds.Bottom;
            int newTrailingEdge = verticalDivider ? resizedBounds.Right : resizedBounds.Bottom;
            int fixedEdgeChange = leadingPane
                ? Math.Abs(newLeadingEdge - oldLeadingEdge)
                : Math.Abs(newTrailingEdge - oldTrailingEdge);
            int dividerEdgeChange = leadingPane
                ? Math.Abs(newTrailingEdge - oldTrailingEdge)
                : Math.Abs(newLeadingEdge - oldLeadingEdge);
            if (fixedEdgeChange > edgeTolerance || dividerEdgeChange <= edgeTolerance)
            {
                return false;
            }

            int resizedDividerEdge = leadingPane ? newTrailingEdge : newLeadingEdge;
            int desiredSplit = leadingPane
                ? resizedDividerEdge - layoutStart - borderSize / 2 + padding + layoutPadding
                : resizedDividerEdge - layoutStart + borderSize / 2 - padding;

            desiredSplit = Math.Clamp(desiredSplit, minimumSplit, maximumSplit);
            int minimumFactor = (int)Math.Ceiling((minimumSplit - baseline) / (double)step);
            int maximumFactor = (int)Math.Floor((maximumSplit - baseline) / (double)step);
            if (maximumFactor < minimumFactor)
            {
                return false;
            }

            factor = (int)Math.Round((desiredSplit - baseline) / (double)step, MidpointRounding.AwayFromZero);
            factor = Math.Clamp(factor, minimumFactor, maximumFactor);
            return true;
        }

        public bool TryAdjustTallSplitFromResize(DesktopWindow oldDesktopWindow, DesktopWindow resizedDesktopWindow)
        {
            Pair<VirtualDesktop, HMONITOR> desktopMonitor = oldDesktopWindow.GetDesktopMonitor();
            if (!desktopMonitor.Equals(resizedDesktopWindow.GetDesktopMonitor()) ||
                !Windows.TryGetValue(desktopMonitor, out ObservableCollection<DesktopWindow> windows) ||
                !windows.Any(window => window.PaneIndex == 0) ||
                !windows.Any(window => window.PaneIndex == 1))
            {
                return false;
            }

            ViewModelDesktopMonitor monitor = mainWindowViewModel.DesktopMonitors.FirstOrDefault(item =>
                item.Monitor.Equals(desktopMonitor.Value) && item.VirtualDesktop.Equals(desktopMonitor.Key));
            if (monitor == null || monitor.Layout != Layout.Tall)
            {
                return false;
            }

            User32.MONITORINFO monitorInfo = new User32.MONITORINFO();
            monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);
            if (!User32.GetMonitorInfo(desktopMonitor.Value, ref monitorInfo))
            {
                return false;
            }

            int layoutX = monitorInfo.rcWork.X + mainWindowViewModel.MarginLeft;
            int layoutWidth = monitorInfo.rcWork.Width - mainWindowViewModel.MarginLeft - mainWindowViewModel.MarginRight;
            Rectangle oldBounds = ToRectangle(oldDesktopWindow.Info.rcWindow);
            Rectangle resizedBounds = ToRectangle(resizedDesktopWindow.Info.rcWindow);

            if (!TryCalculateTallSplitFactor(
                oldBounds,
                resizedBounds,
                oldDesktopWindow.PaneIndex,
                layoutX,
                layoutWidth,
                resizedDesktopWindow.BorderX,
                mainWindowViewModel.Padding,
                mainWindowViewModel.LayoutPadding,
                mainWindowViewModel.Step,
                out int factor))
            {
                return false;
            }

            // Setting even the current value requests a redraw, snapping the divider to the
            // nearest configured step when the user's drag was smaller than one step.
            monitor.Factor = factor;
            return true;
        }

        internal static bool TryCalculateTallSplitFactor(
            Rectangle oldBounds,
            Rectangle resizedBounds,
            int paneIndex,
            int layoutX,
            int layoutWidth,
            int borderX,
            int padding,
            int layoutPadding,
            int step,
            out int factor)
        {
            factor = 0;
            if (layoutWidth < 3 || step <= 0)
            {
                return false;
            }

            const int edgeTolerance = 2;
            int desiredMasterWidth;
            if (paneIndex == 0 &&
                Math.Abs(resizedBounds.Left - oldBounds.Left) <= edgeTolerance &&
                Math.Abs(resizedBounds.Right - oldBounds.Right) > edgeTolerance)
            {
                desiredMasterWidth = resizedBounds.Right - layoutX - borderX / 2 + padding;
            }
            else if (paneIndex == 1 &&
                Math.Abs(resizedBounds.Right - oldBounds.Right) <= edgeTolerance &&
                Math.Abs(resizedBounds.Left - oldBounds.Left) > edgeTolerance)
            {
                desiredMasterWidth = resizedBounds.Left - layoutX + borderX / 2 - padding - layoutPadding;
            }
            else
            {
                return false;
            }

            int minimumPaneWidth = Math.Min(120, Math.Max(1, layoutWidth / 4));
            desiredMasterWidth = Math.Clamp(desiredMasterWidth, minimumPaneWidth, layoutWidth - minimumPaneWidth);
            factor = (int)Math.Round(
                (desiredMasterWidth - layoutWidth / 2.0) / step,
                MidpointRounding.AwayFromZero);

            int minimumFactor = (int)Math.Ceiling((minimumPaneWidth - layoutWidth / 2.0) / step);
            int maximumFactor = (int)Math.Floor((layoutWidth - minimumPaneWidth - layoutWidth / 2.0) / step);
            factor = Math.Clamp(factor, minimumFactor, maximumFactor);
            return true;
        }

        private static Rectangle ToRectangle(Vanara.PInvoke.RECT bounds)
        {
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public void ClearWindows()
        {
            foreach (var desktopMonitor in Windows)
            {
                desktopMonitor.Value.Clear();
            }
            ActivePaneIndices.Clear();
        }

        public List<DesktopWindow> GetWindowsByVirtualDesktop(VirtualDesktop virtualDesktop)
        {
            IEnumerable<Pair<VirtualDesktop, HMONITOR>> desktopMonitorPairs = Windows.Keys.Where(desktopMonitor => desktopMonitor.Key.Equals(virtualDesktop));
            return Windows.Where(windowsList => desktopMonitorPairs.Contains(windowsList.Key)).Select(windowsList => windowsList.Value).SelectMany(window => window).ToList();
        }

        public void CollectWindows()
        {
            List<DesktopWindow> collectedWindows = new List<DesktopWindow>();
            HWND foregroundWindow = User32.GetForegroundWindow();
            User32.EnumWindowsProc filterDesktopWindows = delegate (HWND windowHandle, IntPtr lparam)
            {
                DesktopWindow desktopWindow = new DesktopWindow(windowHandle);
                desktopWindow.GetAppName();
                desktopWindow.GetClassName();

                Pair<string, string> configurableAddition = mainWindowViewModel.ConfigurableAdditions.FirstOrDefault(f => f.Key == desktopWindow.AppName);
                bool hasActiveAddition;
                if (!(configurableAddition.Key == null))
                {
                    hasActiveAddition = configurableAddition.Value.Equals("*") || configurableAddition.Value.Equals(desktopWindow.ClassName);
                }
                else
                {
                    hasActiveAddition = false;
                }

                if (desktopWindow.IsRuntimePresent() || hasActiveAddition)
                {
                    if (User32.IsIconic(windowHandle) || !User32.IsWindowVisible(windowHandle)) return true;
                    desktopWindow.GetInfo();
                    collectedWindows.Add(desktopWindow);
                } else
                {
                    if (desktopWindow.IsExcluded() && !ExcludedWindows.Contains(desktopWindow) && desktopWindow.AppName != "" && !FixedExcludedFilters.Contains(desktopWindow.AppName)) ExcludedWindows.Add(desktopWindow);
                }
                return true;
            };

            User32.EnumWindows(filterDesktopWindows, IntPtr.Zero);

            foreach (IGrouping<Pair<VirtualDesktop, HMONITOR>, DesktopWindow> group in collectedWindows.GroupBy(window => window.GetDesktopMonitor()))
            {
                Pair<VirtualDesktop, HMONITOR> key = group.Key;
                List<DesktopWindow> groupWindows = group.ToList();
                DesktopWindow active = groupWindows.FirstOrDefault(window => window.Window == foregroundWindow) ?? groupWindows[0];
                if (!Windows.ContainsKey(key))
                {
                    Windows[key] = new ObservableCollection<DesktopWindow>();
                }
                else
                {
                    Windows[key].Clear();
                }
                ActivePaneIndices[key] = 0;

                active.PaneIndex = 0;
                active.StackOrder = 0;
                Windows[key].Add(active);

                int stackIndex = 0;
                int paneCount = GetPaneCount(key);
                int[] paneStackOrders = new int[paneCount];
                paneStackOrders[0] = 1;
                foreach (DesktopWindow window in groupWindows.Where(window => window.Window != active.Window))
                {
                    int pane = (stackIndex + 1) % paneCount;
                    window.PaneIndex = pane;
                    window.StackOrder = paneStackOrders[pane]++;
                    Windows[key].Add(window);
                    stackIndex++;
                }
            }

            foreach (var desktopMonitor in Windows)
            {
                if (!WindowsSubscribed.ContainsKey(desktopMonitor.Key))
                {
                    WindowsSubscribed.Add(desktopMonitor.Key, false);
                }
                SubscribeWindowsCollectionChanged(desktopMonitor.Key, true);
            }
        }

        private void Windows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action.Equals(NotifyCollectionChangedAction.Remove))
            {
                DesktopWindow desktopWindow = (DesktopWindow)e.OldItems[0];
            }
            else if (e.Action.Equals(NotifyCollectionChangedAction.Add))
            {
                DesktopWindow desktopWindow = (DesktopWindow)e.NewItems[0];
                if (desktopWindow.GetDesktopMonitor().Key.Equals(null) || desktopWindow.GetDesktopMonitor().Value.Equals(null)) desktopWindow.GetInfo();
            }
            if (!App.Current.MainWindow.Equals(null))
            {
                mainWindowViewModel.UpdateWindows();
            }
        }

        public DesktopWindow FindWindow(HWND hWND)
        {
            List<DesktopWindow> desktopWindows = new List<DesktopWindow>();
            foreach (var desktopMonitor in Windows)
            {
                desktopWindows.AddRange(Windows[new Pair<VirtualDesktop, HMONITOR>(desktopMonitor.Key.Key, desktopMonitor.Key.Value)].Where(window => window.Window == hWND));
            }
            return desktopWindows.FirstOrDefault();
        }
        
        public DesktopWindow GetWindowByHandlers(HWND hWND, HMONITOR hMONITOR, VirtualDesktop desktop)
        {
            return Windows[new Pair<VirtualDesktop, HMONITOR>(desktop, hMONITOR)].First(window => window.Window == hWND);
        }
          
        public void SetMainWindow(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            Windows[desktopMonitor].Move(
                    Windows[desktopMonitor].IndexOf(window),
                    0
                    );
        }

        public void RotateFocusedWindowClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == maxIndex)
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][0].Window);
            }
            else
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][++currentIndex].Window);
            }
        }

        public void RotateFocusedWindowCounterClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == 0)
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][maxIndex].Window);
            }
            else
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][--currentIndex].Window);
            }
        }

        public void MoveWindowClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == maxIndex)
            {
                Windows[desktopMonitor].Move(currentIndex, 0);
            }
            else
            {
                Windows[desktopMonitor].Move(currentIndex, ++currentIndex);
            }
        }

        public void MoveWindowCounterClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == 0)
            {
                Windows[desktopMonitor].Move(currentIndex, maxIndex);
            }
            else
            {
                Windows[desktopMonitor].Move(currentIndex, --currentIndex);
            }
        }

        public void MoveWindowNextScreen(DesktopWindow window)
        {
            List<Pair<VirtualDesktop, HMONITOR>> desktopMonitors = Windows.Keys.Where(dM => dM.Key.ToString() == VirtualDesktop.Current.ToString()).ToList();
            int currentMonitorIndex = desktopMonitors.IndexOf(window.GetDesktopMonitor());
            int maxIndex = desktopMonitors.Count - 1;
            mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
            if (currentMonitorIndex == maxIndex)
            {
                RemoveWindow(window);
                mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[0].Value;
                AddWindow(window);
            }
            else
            {
                RemoveWindow(window);
                mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[++currentMonitorIndex].Value;
                AddWindow(window);
            }
        }

        public void MoveWindowPreviousScreen(DesktopWindow window)
        {
            List<Pair<VirtualDesktop, HMONITOR>> desktopMonitors = Windows.Keys.Where(dM => dM.Key.ToString() == VirtualDesktop.Current.ToString()).ToList();
            int currentMonitorIndex = desktopMonitors.IndexOf(window.GetDesktopMonitor());
            int maxIndex = desktopMonitors.Count - 1;
            mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
            if (currentMonitorIndex == 0)
            {
                RemoveWindow(window);
                mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[maxIndex].Value;
                AddWindow(window);
            }
            else
            {
                RemoveWindow(window);
                mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[--currentMonitorIndex].Value;
                AddWindow(window);
            }
        }

        public void MoveWindowNextVirtualDesktop(DesktopWindow window)
        {
            VirtualDesktop nextVirtualDesktop = window.VirtualDesktop.GetRight();
            if (nextVirtualDesktop != null)
            {
                RemoveWindow(window);
                VirtualDesktop.MoveToDesktop(window.Window.DangerousGetHandle(), nextVirtualDesktop);
                window.VirtualDesktop = nextVirtualDesktop;
                AddWindow(window);
                nextVirtualDesktop.Switch();
            }
        }

        public void MoveWindowPreviousVirtualDesktop(DesktopWindow window)
        {
            VirtualDesktop nextVirtualDesktop = window.VirtualDesktop.GetLeft();
            if (nextVirtualDesktop != null) 
            {
                RemoveWindow(window);
                VirtualDesktop.MoveToDesktop(window.Window.DangerousGetHandle(), nextVirtualDesktop);
                window.VirtualDesktop = nextVirtualDesktop;
                AddWindow(window);
                nextVirtualDesktop.Switch();
            }
        }

        public void MoveWindowSpecificVirtualDesktop(DesktopWindow window, Guid id)
        {
            VirtualDesktop nextVirtualDesktop = VirtualDesktop.FromId(id);
            if (nextVirtualDesktop != null)
            {
                RemoveWindow(window);
                VirtualDesktop.MoveToDesktop(window.Window.DangerousGetHandle(), nextVirtualDesktop);
                window.VirtualDesktop = nextVirtualDesktop;
                AddWindow(window);
                nextVirtualDesktop.Switch();
            }
        }
    }
}
