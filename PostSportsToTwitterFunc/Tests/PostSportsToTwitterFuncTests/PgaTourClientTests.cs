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
    public class PgaTourClientTests
    {
        private static readonly Mock<ILogger> MockLogger = new Mock<ILogger>();

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetFormattedLeaderboardAsync_NotFinalRoundComplete_ReturnLeaderboard()
        {
            var expectedLeaderboard = GetExpectedLeaderboard();
            var expectedTournament = "Farmers Insurance Open";
            var expectedRound = "Round 2";

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourCurrentTournament.json"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourUserTrackingIdScript.js"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourLeaderboardNotFinalRoundComplete.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new PgaTourClient(MockLogger.Object, httpClient);

                var leaderboard = await client.GetFormattedLeaderboardAsync();
                var requests = fakeHandler.GetRequests();

                leaderboard.Should().Contain(expectedLeaderboard);
                leaderboard.Should().Contain(expectedTournament);
                leaderboard.Should().Contain(expectedRound);
                requests.Should().NotBeEmpty()
                    .And.HaveCount(3);
                requests.Where(r => r.RequestUri.ToString().Contains("message")).Should().NotBeEmpty().And.HaveCount(1);
                requests.Where(r => r.RequestUri.ToString().Contains("microservice")).Should().NotBeEmpty().And.HaveCount(1);
                requests.Where(r => r.RequestUri.ToString().Contains("leaderboard")).Should().NotBeEmpty().And.HaveCount(1);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetFormattedLeaderboardAsync_FinalRoundComplete_ReturnLeaderboard()
        {
            var expectedRound = "Final round";

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourCurrentTournament.json"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourUserTrackingIdScript.js"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourLeaderboardFinalRoundComplete.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new PgaTourClient(MockLogger.Object, httpClient);

                var leaderboard = await client.GetFormattedLeaderboardAsync();

                leaderboard.Should().Contain(expectedRound);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetFormattedLeaderboardAsync_RoundInProgress_ReturnNull()
        {
            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourCurrentTournament.json"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourUserTrackingIdScript.js"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\PgaTourLeaderboardRoundInProgress.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new PgaTourClient(MockLogger.Object, httpClient);

                var leaderboard = await client.GetFormattedLeaderboardAsync();

                leaderboard.Should().BeNull();
            }
        }

        private string GetExpectedLeaderboard()
        {
            return 
                "1. Marc Leishman -15" + Environment.NewLine +
                "2. Jon Rahm -14" + Environment.NewLine +
                "T3. Brandt Snedeker -12" + Environment.NewLine +
                "T3. Rory McIlroy -12" + Environment.NewLine +
                "5. Tom Hoge -11";
        }
    }
}
