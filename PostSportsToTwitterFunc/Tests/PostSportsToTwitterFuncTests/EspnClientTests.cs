using System;
using System.Collections.Generic;
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
        private readonly Mock<ILogger> MockLogger = new Mock<ILogger>();

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_TeamNotPlaying_ReturnNull()
        {
            var teams = new List<string>() { "ABC" };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var statuses = await client.GetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                statuses.Should().BeEmpty();
                MockLogger.VerifyLogged(Times.Once());
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GameNotComplete_ReturnNull()
        {
            var teams = new List<string>() { "IND" };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var statuses = await client.GetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                statuses.Should().BeEmpty();
                MockLogger.VerifyLogged(Times.Once());
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GameComplete_ReturnFormattedStatus()
        {
            var teams = new List<string>() { "TOR" };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var statuses = await client.GetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                statuses.Should().NotBeEmpty().And.HaveCount(1);
                Assert.Equal(teams.First(), statuses.First().Key);
                Assert.Equal(GetExpectedTorStatus(), statuses.First().Value);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GamesComplete_ReturnFormattedStatuses()
        {
            var teams = new List<string>() { "TOR", "DEN" };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, httpClient);

                var statuses = await client.GetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                statuses.Should().NotBeEmpty().And.HaveCount(2);
                statuses.Should().Contain(new KeyValuePair<string, string>(teams[0], GetExpectedTorStatus()));
                statuses.Should().Contain(new KeyValuePair<string, string>(teams[1], GetExpectedDenStatus()));
            }
        }

        private string GetExpectedTorStatus()
        {
            return
                "Raptors game is complete." + Environment.NewLine +
                "TOR: 110, SA: 106" + Environment.NewLine +
                "Sunday, January 26, 2020";
        }

        private string GetExpectedDenStatus()
        {
            return
                "Nuggets game is complete." + Environment.NewLine +
                "HOU: 110, DEN: 117" + Environment.NewLine +
                "Sunday, January 26, 2020";
        }

        private FakeHttpMessageHandler GetFakeHandler()
        {
            var handler = new FakeHttpMessageHandler();
            handler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\EspnNbaScoreboard.json"))
            });
            handler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\EspnNbaScoreboard.json"))
            });
            return handler;
        }
    }
}
