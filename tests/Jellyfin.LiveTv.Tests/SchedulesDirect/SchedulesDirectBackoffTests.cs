using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;
using SchedulesDirectProvider = Jellyfin.LiveTv.Listings.SchedulesDirect;

namespace Jellyfin.LiveTv.Tests.SchedulesDirect
{
    public sealed class SchedulesDirectBackoffTests : IDisposable
    {
        private readonly string _cachePath;

        public SchedulesDirectBackoffTests()
        {
            _cachePath = Path.Combine(Path.GetTempPath(), "jf-sd-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_cachePath);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_cachePath, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup.
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// SD returns HTTP 200 with a recognized transient error code (SERVICE_OFFLINE).
        /// The provider must back off after the first token request instead of
        /// re-authenticating once per channel.
        /// </summary>
        [Fact]
        public async Task GetPrograms_HttpOkWithTransientErrorBody_BacksOffAfterSingleTokenRequest()
        {
            const string Body = "{\"response\":\"SERVICE_OFFLINE\",\"code\":3000,\"message\":\"Server offline for maintenance.\"}";
            var (provider, tokenRequests) = CreateProvider(HttpStatusCode.OK, Body);

            await SimulateGuideRefresh(provider, channelCount: 5);

            Assert.Equal(1, tokenRequests.Count);
        }

        /// <summary>
        /// SD returns HTTP 200 with an error code that is NOT modelled by SdErrorCode
        /// (9999 = HCF). The provider must still treat it as a failure and back off rather
        /// than deserializing the error body as a successful response.
        /// </summary>
        [Fact]
        public async Task GetPrograms_HttpOkWithUnknownErrorBody_BacksOffAfterSingleTokenRequest()
        {
            const string Body = "{\"response\":\"ERROR\",\"code\":9999,\"message\":\"Something unexpected.\"}";
            var (provider, tokenRequests) = CreateProvider(HttpStatusCode.OK, Body);

            await SimulateGuideRefresh(provider, channelCount: 5);

            Assert.Equal(1, tokenRequests.Count);
        }

        /// <summary>
        /// The transient backoff must survive a restart so a crash/auto-update loop
        /// cannot reset the backoff and start a fresh authentication storm.
        /// </summary>
        [Fact]
        public async Task GetPrograms_AfterBackoffPersisted_NewInstanceDoesNotReauthenticate()
        {
            const string Body = "{\"response\":\"SERVICE_OFFLINE\",\"code\":3000,\"message\":\"Server offline for maintenance.\"}";
            var (firstProvider, firstRequests) = CreateProvider(HttpStatusCode.OK, Body);

            await SimulateGuideRefresh(firstProvider, channelCount: 1);
            Assert.Equal(1, firstRequests.Count);

            // A new instance pointed at the same cache path simulates a server restart.
            var (secondProvider, secondRequests) = CreateProvider(HttpStatusCode.OK, Body);
            await SimulateGuideRefresh(secondProvider, channelCount: 5);

            Assert.Equal(0, secondRequests.Count);
        }

        private static async Task SimulateGuideRefresh(SchedulesDirectProvider provider, int channelCount)
        {
            var info = new ListingsProviderInfo { Username = "user", Password = "pass" };
            var start = DateTime.UtcNow;
            var end = start.AddDays(1);

            // Mirror the GuideManager loop, which swallows per-channel exceptions
            // and continues to the next channel.
            for (var i = 0; i < channelCount; i++)
            {
                try
                {
                    await provider.GetProgramsAsync(info, "channel" + i, start, end, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Expected for the failing-token cases; the loop must continue.
                }
            }
        }

        private (SchedulesDirectProvider Provider, RequestLog TokenRequests) CreateProvider(HttpStatusCode statusCode, string body)
        {
            var tokenRequests = new RequestLog();

            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    if (request.RequestUri is not null
                        && request.RequestUri.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
                    {
                        tokenRequests.Count++;
                    }

                    return Task.FromResult(new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(body)
                    });
                });

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(handler.Object));

            var appPaths = new Mock<IApplicationPaths>();
            appPaths.SetupGet(x => x.CachePath).Returns(_cachePath);

            var provider = new SchedulesDirectProvider(
                NullLogger<SchedulesDirectProvider>.Instance,
                httpClientFactory.Object,
                appPaths.Object);

            return (provider, tokenRequests);
        }

        private sealed class RequestLog
        {
            public int Count { get; set; }
        }
    }
}
