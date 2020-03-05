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
        private readonly FakeClock FakeClock = new FakeClock();

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SetGameStatusesAsync_IncludesDates()
        {
            var teams = new List<Team>() { new Team() { TeamAbbreviation = "ABC" } };
            var fakeHandler = GetFakeHandler();
            FakeClock.UtcNow = new DateTime(2019, 10, 20);
            FakeClock.Now = new DateTimeOffset(FakeClock.UtcNow);

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, FakeClock, httpClient);

                await client.SetGameStatusesAsync(EspnClient.Sport.NBA, teams);
            }

            var requests = fakeHandler.GetRequests();
            requests.Should().NotBeEmpty().And.HaveCount(1);
            var queryString = requests.First().RequestUri.Query;
            var queryCollection = System.Web.HttpUtility.ParseQueryString(queryString);
            queryCollection.Get("dates").Should().Be("20191019");
            queryCollection[queryCollection.Count - 1].Should().Be(FakeClock.Now.ToUnixTimeSeconds().ToString());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SetGameStatusesAsync_TeamNotPlaying_ReturnNull()
        {
            var teams = new List<Team>() { new Team() { TeamAbbreviation = "ABC" } };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, FakeClock, httpClient);

                await client.SetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                teams.First().Status.Should().BeNull();
                MockLogger.VerifyLogged(Times.Once());
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SetGameStatusesAsync_GameNotComplete_ReturnNull()
        {
            var teams = new List<Team>() { new Team() { TeamAbbreviation = "IND" } };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, FakeClock, httpClient);

                await client.SetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                teams.First().Status.Should().BeNull();
                MockLogger.VerifyLogged(Times.Once());
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SetGameStatusesAsync_GameComplete_ReturnFormattedStatus()
        {
            var teams = new List<Team>() { new Team() { TeamAbbreviation = "TOR" } };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, FakeClock, httpClient);

                await client.SetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                teams.First().Status.Should().Be(GetExpectedTorStatus());
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SetGameStatusesAsync_GamesComplete_ReturnFormattedStatuses()
        {
            var teams = new List<Team>() 
            { 
                new Team() { TeamAbbreviation = "TOR" }, 
                new Team() { TeamAbbreviation = "DEN" }
            };
            var fakeHandler = GetFakeHandler();

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new EspnClient(MockLogger.Object, FakeClock, httpClient);

                await client.SetGameStatusesAsync(EspnClient.Sport.NBA, teams);

                teams[0].Status.Should().Be(GetExpectedTorStatus());
                teams[1].Status.Should().Be(GetExpectedDenStatus());
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
