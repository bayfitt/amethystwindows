using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Vanara.PInvoke;

namespace AmethystWindows.DesktopWindowsManager
{
    internal class Hooks
    {
        public DesktopWindowsManager DesktopWindowsManager { get; set; }

        private ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        ILogger logger;

        public Hooks(DesktopWindowsManager desktopWindowsManager)
        {
            logger = loggerFactory.CreateLogger<Hooks>();
            DesktopWindowsManager = desktopWindowsManager;
        }

        private async void ManageShown(HWND hWND)
        {
            await Task.Delay(500);
            DesktopWindow desktopWindow = new DesktopWindow(hWND);
            desktopWindow.GetInfo();
            DesktopWindowsManager.mainWindowViewModel.LastChangedDesktopMonitor = desktopWindow.GetDesktopMonitor();
            Pair<string, string> configurableAddition = DesktopWindowsManager.mainWindowViewModel.ConfigurableAdditions.FirstOrDefault(f => f.Key == desktopWindow.AppName);
            bool hasActiveAddition;
            if (!(configurableAddition.Key == null))
            {
                hasActiveAddition = configurableAddition.Value.Equals("*") || configurableAddition.Value.Equals(desktopWindow.ClassName);
            } else
            {
                hasActiveAddition = false;
            }         
            if (desktopWindow.IsRuntimePresent() || hasActiveAddition)
            {
                Debug.WriteLine($"window created");
                bool isForeground = hWND == User32.GetForegroundWindow();
                if (isForeground) DesktopWindowsManager.ActivateWindow(desktopWindow);
                else DesktopWindowsManager.AddWindow(desktopWindow, false);
            } else
            {
                if (desktopWindow.IsExcluded() && !DesktopWindowsManager.ExcludedWindows.Contains(desktopWindow) && desktopWindow.AppName != "" && !DesktopWindowsManager.FixedExcludedFilters.Contains(desktopWindow.AppName)) DesktopWindowsManager.ExcludedWindows.Add(desktopWindow);
            }
        }

        public void setWindowsHook()
        {
            void WinEventHookAll(User32.HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
            {
                if (hwnd == HWND.NULL)
                {
                    return;
                }

                // Destroy notifications can arrive after the HWND has stopped identifying
                // a normal window object, so process them before checking idObject/idChild.
                if (winEvent == User32.EventConstants.EVENT_OBJECT_DESTROY)
                {
                    RemoveManagedWindow(hwnd, "destroyed");
                    return;
                }

                if (idObject != User32.ObjectIdentifiers.OBJID_WINDOW || idChild != 0)
                {
                    return;
                }

                // Close and minimize events can make IsRuntimeValuable false before the
                // normal event filter runs. Remove these HWNDs first so the remaining
                // Tall pane is immediately laid out across the full work area.
                if (winEvent == User32.EventConstants.EVENT_SYSTEM_MINIMIZESTART ||
                    winEvent == User32.EventConstants.EVENT_OBJECT_HIDE ||
                    winEvent == User32.EventConstants.EVENT_OBJECT_IME_HIDE)
                {
                    RemoveManagedWindow(hwnd, "hidden or minimized");
                    return;
                }

                DesktopWindow desktopWindow = new DesktopWindow(hwnd);
                if (desktopWindow.IsRuntimeValuable())
                {
                    switch (winEvent)
                    {
                        case User32.EventConstants.EVENT_OBJECT_SHOW:
                        case User32.EventConstants.EVENT_OBJECT_UNCLOAKED:
                        case User32.EventConstants.EVENT_OBJECT_IME_SHOW:
                        case User32.EventConstants.EVENT_SYSTEM_FOREGROUND:
                            ManageShown(hwnd);
                            break;
                        case User32.EventConstants.EVENT_SYSTEM_MINIMIZEEND:
                            Debug.WriteLine($"window maximized");
                            desktopWindow.GetInfo();
                            DesktopWindowsManager.mainWindowViewModel.LastChangedDesktopMonitor = desktopWindow.GetDesktopMonitor();
                            Pair<string, string> configurableAddition = DesktopWindowsManager.mainWindowViewModel.ConfigurableAdditions.FirstOrDefault(f => f.Key == desktopWindow.AppName);
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
                                bool isForeground = hwnd == User32.GetForegroundWindow();
                                if (isForeground) DesktopWindowsManager.ActivateWindow(desktopWindow);
                                else DesktopWindowsManager.AddWindow(desktopWindow, false);
                            }
                            break;
                        case User32.EventConstants.EVENT_SYSTEM_MOVESIZEEND:
                            Debug.WriteLine($"window move/size");
                            DesktopWindowsManager.mainWindowViewModel.LastChangedDesktopMonitor = new Pair<WindowsDesktop.VirtualDesktop, HMONITOR>(null, new HMONITOR());
                            DesktopWindow moved = DesktopWindowsManager.FindWindow(hwnd);
                            if (moved != null)
                            {
                                DesktopWindow newMoved = new DesktopWindow(hwnd);
                                newMoved.GetInfo();
                                Pair<WindowsDesktop.VirtualDesktop, HMONITOR> oldDesktopMonitor = moved.GetDesktopMonitor();
                                Pair<WindowsDesktop.VirtualDesktop, HMONITOR> newDesktopMonitor = newMoved.GetDesktopMonitor();
                                if (!oldDesktopMonitor.Equals(newDesktopMonitor))
                                {
                                    DesktopWindowsManager.RepositionWindow(moved, newMoved);
                                }
                                else if (!DesktopWindowsManager.TryAdjustSharedSplitFromResize(moved, newMoved))
                                {
                                    // Reapply the active layout after moving a window or resizing an
                                    // outer edge. Shared dividers update their saved split positions.
                                    DesktopWindowsManager.Draw(oldDesktopMonitor);
                                }
                            }
                            break;
                        case User32.EventConstants.EVENT_OBJECT_DRAGCOMPLETE:
                            Debug.WriteLine($"window dragged");
                            break;
                        default:
                            break;
                    }
                }
            }

            User32.WinEventProc winEventHookAll = new User32.WinEventProc(WinEventHookAll);
            GCHandle gchCreate = GCHandle.Alloc(winEventHookAll);

            User32.HWINEVENTHOOK hookAll = User32.SetWinEventHook(User32.EventConstants.EVENT_MIN, User32.EventConstants.EVENT_MAX, HINSTANCE.NULL, winEventHookAll, 0, 0, User32.WINEVENT.WINEVENT_OUTOFCONTEXT | User32.WINEVENT.WINEVENT_SKIPOWNPROCESS);
        }

        private void RemoveManagedWindow(HWND hwnd, string reason)
        {
            DesktopWindow removed = DesktopWindowsManager.FindWindow(hwnd);
            if (removed == null)
            {
                return;
            }

            Debug.WriteLine($"window {reason}");
            DesktopWindowsManager.mainWindowViewModel.LastChangedDesktopMonitor = removed.GetDesktopMonitor();
            DesktopWindowsManager.RemoveWindow(removed);
        }

        private User32.HotKeyModifiers convertModifiers(ViewModelHotkey viewModelHotkey)
        {
            User32.HotKeyModifiers modifiers = 0;

            if (viewModelHotkey.Hotkey.Modifiers.HasFlag(ModifierKeys.Control))
                modifiers |= User32.HotKeyModifiers.MOD_CONTROL;
            if (viewModelHotkey.Hotkey.Modifiers.HasFlag(ModifierKeys.Shift))
                modifiers |= User32.HotKeyModifiers.MOD_SHIFT;
            if (viewModelHotkey.Hotkey.Modifiers.HasFlag(ModifierKeys.Alt))
                modifiers |= User32.HotKeyModifiers.MOD_ALT;
            if (viewModelHotkey.Hotkey.Modifiers.HasFlag(ModifierKeys.Windows))
                modifiers |= User32.HotKeyModifiers.MOD_WIN;

            return modifiers;
        }

        public void setKeyboardHook(HWND hWND, ObservableCollection<ViewModelHotkey> hotkeys)
        {
            User32.RegisterHotKey(hWND, 0x11, convertModifiers(hotkeys[0]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[0].Hotkey.Key)); //space
            User32.RegisterHotKey(hWND, 0x21, convertModifiers(hotkeys[1]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[1].Hotkey.Key)); //space
            User32.RegisterHotKey(hWND, 0x12, convertModifiers(hotkeys[2]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[2].Hotkey.Key)); //enter
            User32.RegisterHotKey(hWND, 0x13, convertModifiers(hotkeys[3]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[3].Hotkey.Key)); //H
            User32.RegisterHotKey(hWND, 0x16, convertModifiers(hotkeys[4]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[4].Hotkey.Key)); //L
            User32.RegisterHotKey(hWND, 0x14, convertModifiers(hotkeys[5]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[5].Hotkey.Key)); //J 
            User32.RegisterHotKey(hWND, 0x15, convertModifiers(hotkeys[6]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[6].Hotkey.Key)); //K
            User32.RegisterHotKey(hWND, 0x18, convertModifiers(hotkeys[7]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[7].Hotkey.Key)); //P
            User32.RegisterHotKey(hWND, 0x19, convertModifiers(hotkeys[8]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[8].Hotkey.Key)); //N
            User32.RegisterHotKey(hWND, 0x22, convertModifiers(hotkeys[9]),  (uint)KeyInterop.VirtualKeyFromKey(hotkeys[9].Hotkey.Key)); //L
            User32.RegisterHotKey(hWND, 0x23, convertModifiers(hotkeys[10]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[10].Hotkey.Key)); //H
            User32.RegisterHotKey(hWND, 0x25, convertModifiers(hotkeys[11]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[11].Hotkey.Key)); //K
            User32.RegisterHotKey(hWND, 0x24, convertModifiers(hotkeys[12]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[12].Hotkey.Key)); //J
            User32.RegisterHotKey(hWND, 0x17, convertModifiers(hotkeys[13]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[13].Hotkey.Key)); //Z
            User32.RegisterHotKey(hWND, 0x26, convertModifiers(hotkeys[14]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[14].Hotkey.Key)); //right
            User32.RegisterHotKey(hWND, 0x27, convertModifiers(hotkeys[15]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[15].Hotkey.Key)); //left
            User32.RegisterHotKey(hWND, 0x1,  convertModifiers(hotkeys[16]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[16].Hotkey.Key)); //1
            User32.RegisterHotKey(hWND, 0x2,  convertModifiers(hotkeys[17]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[17].Hotkey.Key)); //2
            User32.RegisterHotKey(hWND, 0x3,  convertModifiers(hotkeys[18]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[18].Hotkey.Key)); //3
            User32.RegisterHotKey(hWND, 0x4,  convertModifiers(hotkeys[19]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[19].Hotkey.Key)); //4
            User32.RegisterHotKey(hWND, 0x5,  convertModifiers(hotkeys[20]), (uint)KeyInterop.VirtualKeyFromKey(hotkeys[20].Hotkey.Key)); //5
        }

        public void unsetKeyboardHook(HWND hWND)
        {
            User32.UnregisterHotKey(hWND, 0x11); //space
            User32.UnregisterHotKey(hWND, 0x21); //space
            User32.UnregisterHotKey(hWND, 0x12); //enter
            User32.UnregisterHotKey(hWND, 0x13); //H
            User32.UnregisterHotKey(hWND, 0x16); //L
            User32.UnregisterHotKey(hWND, 0x14); //J 
            User32.UnregisterHotKey(hWND, 0x15); //K
            User32.UnregisterHotKey(hWND, 0x18); //P
            User32.UnregisterHotKey(hWND, 0x19); //N
            User32.UnregisterHotKey(hWND, 0x23); //L
            User32.UnregisterHotKey(hWND, 0x22); //H
            User32.UnregisterHotKey(hWND, 0x25); //K
            User32.UnregisterHotKey(hWND, 0x24); //J
            User32.UnregisterHotKey(hWND, 0x17); //Z
            User32.UnregisterHotKey(hWND, 0x26); //right
            User32.UnregisterHotKey(hWND, 0x27); //left
            User32.UnregisterHotKey(hWND, 0x1); //1
            User32.UnregisterHotKey(hWND, 0x2); //2
            User32.UnregisterHotKey(hWND, 0x3); //3
            User32.UnregisterHotKey(hWND, 0x4); //4
            User32.UnregisterHotKey(hWND, 0x5); //5
        }
    }
}
