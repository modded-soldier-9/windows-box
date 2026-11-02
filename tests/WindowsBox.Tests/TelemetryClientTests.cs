using System.Net;
using WindowsBox.Core.Settings;
using WindowsBox.Core.Telemetry;

namespace WindowsBox.Tests;

public class TelemetryClientTests
{
    [Fact]
    public async Task Track_is_noop_when_disabled()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-tel-" + Guid.NewGuid().ToString("N"));
        var calls = 0;
        try
        {
            var store = new FileAppSettingsStore(dir);
            var client = new TelemetryClient(
                store,
                httpClientFactory: () =>
                {
                    calls++;
                    return new HttpClient(new OkHandler());
                },
                endpoint: "https://example.test/t");

            var result = await client.TrackAsync(new TelemetryEvent { Name = "app.open" });
            Assert.True(result.SkippedBecauseDisabled);
            Assert.False(result.Sent);
            Assert.Equal(0, calls);
            Assert.Empty(client.LocalBuffer);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Track_sends_when_enabled()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-tel-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileAppSettingsStore(dir);
            store.Save(new AppSettings { TelemetryEnabled = true });
            var handler = new OkHandler();
            var client = new TelemetryClient(
                store,
                httpClientFactory: () => new HttpClient(handler),
                endpoint: "https://example.test/t");

            var result = await client.TrackAsync(new TelemetryEvent { Name = "app.open" });
            Assert.True(result.Sent);
            Assert.Equal(1, handler.Calls);
            Assert.Single(client.LocalBuffer);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
