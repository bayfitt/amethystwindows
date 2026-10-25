using System.Linq;
using AmethystWindows.DesktopWindowsManager;
using Xunit;

namespace AmethystWindowsTests
{
    public class PendingMonitorDrawsTests
    {
        [Fact]
        public void MonitorDrawStaysOnTheMonitorThatChanged()
        {
            PendingMonitorDraws<string> pending = new PendingMonitorDraws<string>();

            pending.Add("laptop-bottom");

            PendingMonitorDrawRequest<string> request = pending.Take();

            Assert.False(request.DrawAll);
            Assert.Equal(new[] { "laptop-bottom" }, request.Monitors);
        }

        [Fact]
        public void MonitorChangesDuringOneDebounceKeepEveryMonitorKey()
        {
            PendingMonitorDraws<string> pending = new PendingMonitorDraws<string>();

            pending.Add("external-top");
            pending.Add("laptop-bottom");
            pending.Add("external-top");

            PendingMonitorDrawRequest<string> request = pending.Take();

            Assert.False(request.DrawAll);
            Assert.Equal(new[] { "external-top", "laptop-bottom" }, request.Monitors.OrderBy(monitor => monitor));
        }

        [Fact]
        public void FullRedrawSupersedesMonitorScopedRequests()
        {
            PendingMonitorDraws<string> pending = new PendingMonitorDraws<string>();

            pending.Add("laptop-bottom");
            pending.AddAll();
            pending.Add("external-top");

            PendingMonitorDrawRequest<string> request = pending.Take();

            Assert.True(request.DrawAll);
            Assert.Empty(request.Monitors);
        }
    }
}
