using AmethystWindows.DesktopWindowsManager;
using System;
using Xunit;

namespace AmethystWindowsTests
{
    public class WindowFilterTests
    {
        [Theory]
        [InlineData("AmethystWindows", "HwndWrapper[AmethystWindows]", false)]
        [InlineData("AmethystWindowsPackaging", "Window", false)]
        [InlineData("Codex", "Chrome_WidgetWin_1", true)]
        [InlineData("", "Window", false)]
        public void FixedFiltersApplyToWindowsCollectedAtStartup(string appName, string className, bool expected)
        {
            Assert.Equal(expected, DesktopWindowsManager.ShouldManageWindow(
                appName, className, Array.Empty<Pair<string, string>>()));
        }

        [Fact]
        public void ConfiguredFiltersApplyToCollectedWindows()
        {
            var filters = new[] { new Pair<string, string>("Example", "HiddenClass") };
            Assert.False(DesktopWindowsManager.ShouldManageWindow("Example", "HiddenClass", filters));
            Assert.True(DesktopWindowsManager.ShouldManageWindow("Example", "OtherClass", filters));
            Assert.False(DesktopWindowsManager.ShouldManageWindow("Example", "AnyClass",
                new[] { new Pair<string, string>("Example", "*") }));
        }
    }
}
