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
    public class MySportsFeedsClientTests
    {
        private static readonly Mock<ILogger> MockLogger = new Mock<ILogger>();

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GameComplete_ReturnFormattedStatus()
        {
            var team = "CHI";
            var forDate = new DateTime(2019, 12, 9);
            var expectedTeamName = "Bulls";
            var expectedGameId = 53387;
            var expectedScore = 93;

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\DailyGameScheduleSingleResponse.json"))
            });
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(@"TestData\GameBoxscoreSingleResponse.json"))
            });

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new MySportsFeedsClient(MockLogger.Object, httpClient);

                var gameStatus = await client.GetGameStatusAsync(MySportsFeedsClient.Sport.NBA, forDate, team);
                var requests = fakeHandler.GetRequests();

                gameStatus.Should().Contain(expectedTeamName);
                gameStatus.Should().Contain(expectedScore.ToString());
                gameStatus.Should().Contain(forDate.ToLongDateString());
                requests.Should().NotBeEmpty()
                    .And.HaveCount(2);
                requests.Where(r => r.RequestUri.ToString().Contains("schedule")).Should().NotBeEmpty().And.HaveCount(1);
                requests.Where(r => r.RequestUri.ToString().Contains(expectedGameId.ToString())).Should().NotBeEmpty().And.HaveCount(1);
                requests.Where(r => r.RequestUri.ToString().Contains("boxscore")).Should().NotBeEmpty().And.HaveCount(1);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameStatusAsync_GameInProgress_ReturnNull()
        {
            var team = "CHI";
            var forDate = new DateTime(2019, 12, 9);

            var fakeHandler = new FakeHttpMessageHandler();
            fakeHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

            using (var httpClient = new HttpClient(fakeHandler))
            {
                var client = new MySportsFeedsClient(MockLogger.Object, httpClient);

                var gameStatus = await client.GetGameStatusAsync(MySportsFeedsClient.Sport.NBA, forDate, team);

                gameStatus.Should().BeNull();
            }
        }
    }
}
