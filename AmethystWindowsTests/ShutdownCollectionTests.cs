using AmethystWindows.DesktopWindowsManager;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows;
using Xunit;

namespace AmethystWindowsTests
{
    public class ShutdownCollectionTests
    {
        [Fact]
        public void CollectionChangesDoNotRequireAnApplicationAfterShutdown()
        {
            Assert.Null(Application.Current);
            var manager = new DesktopWindowsManager();

            manager.ExcludedWindows.Add(null!);

            var handler = typeof(DesktopWindowsManager).GetMethod(
                "Windows_CollectionChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var removal = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, (object?)null);
            handler.Invoke(manager, new object?[] { manager.Windows, removal });
        }
    }
}
