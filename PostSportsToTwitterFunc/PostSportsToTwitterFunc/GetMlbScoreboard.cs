using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PostSportsToTwitterFunc
{
    public static class GetMlbScoreboard
    {
        private static readonly string MySportsFeedsUsername = Environment.GetEnvironmentVariable("MySportsFeedsUsername");
        private static readonly string MySportsFeedsPassword = Environment.GetEnvironmentVariable("MySportsFeedsPassword");

        private const string SeasonName = "2019-regular";
        private const string Format = "json";
        private static readonly string ForDate = (DateTime.Today - TimeSpan.FromDays(60)).ToString("yyyyMMdd");
        private static readonly Uri MySportsFeedsBaseUri = new Uri("https://api.mysportsfeeds.com");
        private static readonly Uri MySportsFeedsGetMlbScoreboardUri = new Uri(MySportsFeedsBaseUri, $"v1.2/pull/mlb/{SeasonName}/scoreboard.{Format}?fordate={ForDate}");

        private static readonly HttpClient client = new HttpClient();

        [FunctionName("GetMlbScoreboard")]
        public static async Task Run([TimerTrigger("0 0 * * * * ", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MySportsFeedsUsername}:{MySportsFeedsPassword}"));
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);

            try
            {
                string response = await client.GetStringAsync(MySportsFeedsGetMlbScoreboardUri);
                string gameStatuses = ParseScoreboardResponse(response);

                log.LogInformation(gameStatuses);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception thrown when making or parsing web request.");
            }
        }

        private static string ParseScoreboardResponse(string response)
        {
            var teamToLookFor = "MIL";
            var status = "undefined";

            var responseObject = JObject.Parse(response);
            var games = responseObject["scoreboard"]["gameScore"];
            foreach (var game in games)
            {
                if (game["game"]["awayTeam"]["Abbreviation"].ToString() == teamToLookFor ||
                    game["game"]["homeTeam"]["Abbreviation"].ToString() == teamToLookFor)
                {
                    if (game["isUnplayed"].Value<bool>()) status = "unplayed";
                    else if (game["isInProgress"].Value<bool>()) status = "in progress";
                    else if (game["isCompleted"].Value<bool>()) status = $"complete. {ParseScoreboardCompletedGame(game)}";

                    return $"{ForDate} {teamToLookFor} game is {status}.";
                }
            }

            return $"{teamToLookFor} not found.";
        }

        private static string ParseScoreboardCompletedGame(JToken completedGame)
        {
            return $"{completedGame["game"]["awayTeam"]["Abbreviation"]}: {completedGame["awayScore"]}," +
                $"{completedGame["game"]["homeTeam"]["Abbreviation"]}: {completedGame["homeScore"]}";
        }
    }
}
