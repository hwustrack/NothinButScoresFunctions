using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PostSportsToTwitterFunc
{
    public class MySportsFeedsClient : IDisposable
    {
        private const string SeasonName = "2019-regular";
        private const string Format = "json";
        private static readonly string ForDate = (DateTime.Today - TimeSpan.FromDays(60)).ToString("yyyyMMdd");
        private static readonly Uri BaseUri = new Uri("https://api.mysportsfeeds.com");
        private static readonly Uri GetMlbScoreboardUri = new Uri(BaseUri, $"v1.2/pull/mlb/{SeasonName}/scoreboard.{Format}?fordate={ForDate}");
        private static readonly string Username = Environment.GetEnvironmentVariable("MySportsFeedsUsername");
        private static readonly string Password = Environment.GetEnvironmentVariable("MySportsFeedsPassword");

        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public MySportsFeedsClient(ILogger log)
        {
            _httpClient = new HttpClient();
            _log = log;
        }

        public async Task<string> GetGameStatusAsync()
        {
            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);

            string gameStatus = string.Empty;

            try
            {
                var response = await _httpClient.GetStringAsync(GetMlbScoreboardUri);
                gameStatus = ParseScoreboardResponse(response);

                _log.LogInformation(gameStatus);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception thrown when making or parsing web request.");
            }

            return gameStatus;
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
            return $"{completedGame["game"]["awayTeam"]["Abbreviation"]}: {completedGame["awayScore"]}, " +
                $"{completedGame["game"]["homeTeam"]["Abbreviation"]}: {completedGame["homeScore"]}";
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
