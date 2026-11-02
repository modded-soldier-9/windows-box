using System.Net;
using System.Net.Http.Headers;
using System.Text;
using WindowsBox.Core.Settings;
using WindowsBox.Core.Update;

namespace WindowsBox.Tests;

public class UpdateSettingsTests
{
    [Fact]
    public void Defaults_are_manual_and_check_disabled()
    {
        var settings = new AppSettings();
        Assert.False(settings.CheckForUpdatesEnabled);
        Assert.Equal(UpdatePreference.Manual, settings.UpdatePreference);
    }

    [Fact]
    public void Settings_round_trip_on_disk()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-settings-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileAppSettingsStore(dir);
            var loaded = store.Load();
            Assert.False(loaded.CheckForUpdatesEnabled);

            loaded.CheckForUpdatesEnabled = true;
            loaded.UpdatePreference = UpdatePreference.Automatic;
            store.Save(loaded);

            var again = store.Load();
            Assert.True(again.CheckForUpdatesEnabled);
            Assert.Equal(UpdatePreference.Automatic, again.UpdatePreference);
            Assert.True(File.Exists(store.PathOnDisk));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Update_checker_does_not_touch_network_when_disabled()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-upd-" + Guid.NewGuid().ToString("N"));
        var calls = 0;
        try
        {
            var store = new FileAppSettingsStore(dir);
            var checker = new UpdateChecker(
                store,
                currentVersion: "0.1.0",
                httpClientFactory: () =>
                {
                    calls++;
                    return new HttpClient(new ThrowingHandler());
                });

            var result = await checker.CheckAsync(userInitiated: true);
            Assert.True(result.SkippedBecauseDisabled);
            Assert.False(result.Performed);
            Assert.Equal(0, calls);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Manual_preference_skips_background_check_even_when_enabled()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-upd-" + Guid.NewGuid().ToString("N"));
        var calls = 0;
        try
        {
            var store = new FileAppSettingsStore(dir);
            store.Save(new AppSettings
            {
                CheckForUpdatesEnabled = true,
                UpdatePreference = UpdatePreference.Manual,
            });

            var checker = new UpdateChecker(
                store,
                currentVersion: "0.1.0",
                httpClientFactory: () =>
                {
                    calls++;
                    return new HttpClient(new FakeReleaseHandler("v0.2.0"));
                });

            var background = await checker.CheckAsync(userInitiated: false);
            Assert.False(background.Performed);
            Assert.Equal(0, calls);

            var manual = await checker.CheckAsync(userInitiated: true);
            Assert.True(manual.Performed);
            Assert.True(manual.UpdateAvailable);
            Assert.Equal("v0.2.0", manual.LatestTag);
            Assert.Equal(1, calls);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveApiUrl_differs_for_stable_and_beta()
    {
        Assert.Contains("/releases/latest", UpdateChecker.ResolveApiUrl(UpdateChannel.Stable), StringComparison.Ordinal);
        Assert.Equal(
            UpdateChecker.DefaultBetaReleasesApi,
            UpdateChecker.ResolveApiUrl(UpdateChannel.Beta));
    }

    [Fact]
    public async Task Beta_channel_prefers_prerelease_tag()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-upd-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileAppSettingsStore(dir);
            store.Save(new AppSettings
            {
                CheckForUpdatesEnabled = true,
                UpdatePreference = UpdatePreference.Manual,
                UpdateChannel = UpdateChannel.Beta,
            });

            var checker = new UpdateChecker(
                store,
                currentVersion: "0.1.0",
                httpClientFactory: () => new HttpClient(new FakeReleaseListHandler()));

            var result = await checker.CheckAsync(userInitiated: true);
            Assert.True(result.Performed);
            Assert.Equal("v0.2.0-beta.1", result.LatestTag);
            Assert.Contains("beta", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Automatic_preference_checks_on_open_when_opted_in()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-upd-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileAppSettingsStore(dir);
            store.Save(new AppSettings
            {
                CheckForUpdatesEnabled = true,
                UpdatePreference = UpdatePreference.Automatic,
            });

            var checker = new UpdateChecker(
                store,
                currentVersion: "0.2.0",
                httpClientFactory: () => new HttpClient(new FakeReleaseHandler("v0.2.0")));

            var result = await checker.CheckAsync(userInitiated: false);
            Assert.True(result.Performed);
            Assert.False(result.UpdateAvailable);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Network must not be used when checks are disabled.");
    }

    private sealed class FakeReleaseHandler : HttpMessageHandler
    {
        private readonly string _tag;

        public FakeReleaseHandler(string tag) => _tag = tag;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = $$"""{"tag_name":"{{_tag}}","html_url":"https://example.test/release","prerelease":false}""";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class FakeReleaseListHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            const string json =
                """
                [
                  {"tag_name":"v0.1.9","html_url":"https://example.test/r1","prerelease":false},
                  {"tag_name":"v0.2.0-beta.1","html_url":"https://example.test/r2","prerelease":true}
                ]
                """;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
            };
            return Task.FromResult(response);
        }
    }
}
