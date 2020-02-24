using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PostSportsToTwitterFunc;
using Xunit;

namespace PostSportsToTwitterFuncTests
{
    public class EspnClientTests
    {
        private static readonly Mock<ILogger> MockLogger = new Mock<ILogger>();

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_TeamNotPlaying_ReturnNull()
        {
            var team = "ABC";

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\EspnNbaScoreboard.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var status = await client.GetGameStatusAsync(EspnClient.Sport.NBA, team);

                Assert.Null(status);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GameNotComplete_ReturnNull()
        {
            var team = "IND";

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\EspnNbaScoreboard.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var status = await client.GetGameStatusAsync(EspnClient.Sport.NBA, team);

                Assert.Null(status);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GameComplete_ReturnFormattedStatus()
        {
            var team = "TOR";

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\EspnNbaScoreboard.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var status = await client.GetGameStatusAsync(EspnClient.Sport.NBA, team);

                Assert.Equal(GetExpectedStatus(), status);

                var requests = fakeHandler.GetRequests();
                requests.Should().NotBeEmpty().And.HaveCount(1);
            }
        }

        private string GetExpectedStatus()
        {
            return
                "Raptors game is complete." + Environment.NewLine +
                "TOR: 110, SA: 106" + Environment.NewLine +
                "Sunday, January 26, 2020";
        }
    }
}
