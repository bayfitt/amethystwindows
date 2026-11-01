using AmethystWindows.Settings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WindowsDesktop;

namespace AmethystWindows
{

    public partial class App : Application
    {
        internal static DesktopWindowsManager.DesktopWindowsManager DWM = null;
        internal static DesktopWindowsManager.Hooks hooks = null;
        private static bool firstActivation = true;
        private SingleInstance? singleInstance;
        private bool activationPending;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            singleInstance = new SingleInstance("AmethystWindows");
            if (!singleInstance.IsPrimary)
            {
                if (!e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase))
                    singleInstance.RequestActivation();
                Shutdown();
                return;
            }

            singleInstance.Listen(() =>
            {
                if (!Dispatcher.HasShutdownStarted)
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(OpenSettings));
            });
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            singleInstance?.Dispose();
            base.OnExit(e);
        }

        private void OpenSettings()
        {
            if (MainWindow == null || !MainWindow.IsLoaded)
            {
                activationPending = true;
                return;
            }

            activationPending = false;
            if (MainWindow.DataContext is MainWindowViewModel viewModel)
                viewModel.NotifyIconOpenCommand.Execute(null);
            MainWindow.Activate();
        }

        private void App_Activated(object sender, EventArgs e)
        {
            if (firstActivation)
            {
                DWM = new DesktopWindowsManager.DesktopWindowsManager();
                Debug.WriteLine($"getting settings");
                Debug.WriteLine($"setting hooks");
                hooks = new DesktopWindowsManager.Hooks(DWM);
                hooks.setWindowsHook();
                hooks.setKeyboardHook(new WindowInteropHelper(App.Current.MainWindow).Handle, DWM.mainWindowViewModel.Hotkeys);
                Debug.WriteLine($"getting windows");
                DWM.CollectWindows();
                DWM.Draw();

                Debug.WriteLine($"setting virtual desktop change listener");
                VirtualDesktop.CurrentChanged += VirtualDesktop_CurrentChanged;

                Debug.WriteLine($"setting virtual desktops");
                InitVirtualDesktops();

                firstActivation = false;
            }

            if (activationPending)
                Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(OpenSettings));
        }

        public static void InitVirtualDesktops()
        {
            // Window-layout changes must never create or remove the user's Windows virtual desktops.
        }

        private void VirtualDesktop_CurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
        {
            DWM.CollectWindows();
            DWM.Draw();
        }
    }
}
