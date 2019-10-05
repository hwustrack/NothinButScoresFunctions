using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PostSportsToTwitterFunc
{
    public class MySportsFeedsClient : IDisposable
    {
        public enum Sport
        {
            MLB,
            NFL
        };

        private const string SeasonName = "2019-regular";
        private const string Format = "json";
        private static readonly Uri BaseUri = new Uri("https://api.mysportsfeeds.com");
        private static readonly string Username = Environment.GetEnvironmentVariable("MySportsFeedsUsername");
        private static readonly string Password = Environment.GetEnvironmentVariable("MySportsFeedsPassword");

        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public MySportsFeedsClient(ILogger log)
        {
            _httpClient = new HttpClient();
            _log = log;
        }

        public async Task<string> GetGameStatusAsync(Sport sport, DateTime forDate, string teamAbbreviation)
        {
            string forDateString = forDate.ToString("yyyyMMdd", new CultureInfo("en-US"));
            Uri scoreboardUri = new Uri(BaseUri, $"v1.2/pull/{sport}/{SeasonName}/scoreboard.{Format}?fordate={forDateString}");
            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);

            string gameStatus = string.Empty;

            try
            {
                var response = await _httpClient.GetStringAsync(scoreboardUri);
                gameStatus = ParseScoreboardResponse(response, forDateString, teamAbbreviation);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception thrown when making or parsing web request.");
            }

            return gameStatus;
        }

        private string ParseScoreboardResponse(string response, string forDateString, string teamAbbreviation)
        {
            var responseObject = JObject.Parse(response);
            var games = responseObject["scoreboard"]["gameScore"];
            foreach (var game in games ?? Enumerable.Empty<JToken>())
            {
                if (string.Equals(game["game"]["awayTeam"]["Abbreviation"].ToString(), teamAbbreviation, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(game["game"]["homeTeam"]["Abbreviation"].ToString(), teamAbbreviation, StringComparison.OrdinalIgnoreCase))
                {
                    if (game["isUnplayed"].Value<bool>() || game["isInProgress"].Value<bool>())
                    {
                        _log.LogInformation($"{forDateString} {teamAbbreviation} game is unplayed or in progress. Exiting.");
                        return null;
                    }
                    else if (game["isCompleted"].Value<bool>())
                    {
                        return $"{forDateString} {teamAbbreviation} game is complete. {ParseScoreboardCompletedGame(game)}";
                    }
                } 
            }

            _log.LogInformation($"{teamAbbreviation} not found in scoreboard. Exiting.");
            return null;
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
