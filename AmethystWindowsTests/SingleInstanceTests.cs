using AmethystWindows;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AmethystWindowsTests
{
    public class SingleInstanceTests
    {
        [Fact]
        public void AnotherInstanceSignalsThePrimaryInsteadOfTakingOwnership()
        {
            string name = "AmethystWindowsTests-" + Guid.NewGuid();
            using var primary = new SingleInstance(name);
            using var received = new ManualResetEventSlim();
            Assert.True(primary.IsPrimary);
            primary.Listen(received.Set);

            Task.Run(() =>
            {
                using var secondary = new SingleInstance(name);
                Assert.False(secondary.IsPrimary);
                secondary.RequestActivation();
            }).GetAwaiter().GetResult();

            Assert.True(received.Wait(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void ANewInstanceCanStartAfterThePrimaryExits()
        {
            string name = "AmethystWindowsTests-" + Guid.NewGuid();
            using (var first = new SingleInstance(name))
                Assert.True(first.IsPrimary);

            using var next = new SingleInstance(name);
            Assert.True(next.IsPrimary);
        }
    }
}
