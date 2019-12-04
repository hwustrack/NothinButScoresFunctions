using System;
using System.Collections.Generic;
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
            NFL,
            NHL,
            NBA
        };

        private const string SeasonName = "current";
        private const string Format = "json";
        private static readonly Uri BaseUri = new Uri("https://api.mysportsfeeds.com");
        private static readonly string Username = Environment.GetEnvironmentVariable("MySportsFeedsUsername");
        private static readonly string Password = Environment.GetEnvironmentVariable("MySportsFeedsPassword");
        private static readonly Dictionary<Sport, string> SportToBoxscoreScoreKey = new Dictionary<Sport, string>()
        {
            { Sport.NHL, "period" },
            { Sport.NBA, "quarter" },
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public MySportsFeedsClient(ILogger log)
        {
            _log = log;
            _httpClient = new HttpClient();
            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);
        }

        public async Task<string> GetGameStatus2Async(Sport sport, DateTime forDate, string teamAbbreviation)
        {
            string forDateString = GetFormattedDateTime(forDate);
            Uri scheduleUri = new Uri(BaseUri, $"v1.2/pull/{sport}/{SeasonName}/daily_game_schedule.{Format}?team={teamAbbreviation}&fordate={forDateString}");
            
            var response = await _httpClient.GetStringAsync(scheduleUri);
            var gameId = GetGameIdFromScheduleResponse(response);
            if (gameId == null) return null;

            Uri boxScoreUri = new Uri(BaseUri, $"v1.2/pull/{sport}/{SeasonName}/game_boxscore.{Format}?gameid={gameId}&teamstats=none&playerstats=none");

            response = await _httpClient.GetStringAsync(boxScoreUri); // will return 204 if not complete
            var gameStatus = GetGameStatusFromBoxscoreResponse(response, sport, forDate, teamAbbreviation);

            return gameStatus;
        }

        private int? GetGameIdFromScheduleResponse(string response)
        {
            var responseObject = !string.IsNullOrWhiteSpace(response) ? JObject.Parse(response) : null;
            var gameId = responseObject?["dailygameschedule"]?["gameentry"]?[0]?["id"].Value<int>();
            return gameId;
        }

        private string GetGameStatusFromBoxscoreResponse(string response, Sport sport, DateTime forDate, string teamAbbreviation)
        {
            var responseObject = !string.IsNullOrWhiteSpace(response) ? JObject.Parse(response) : null;

            if (responseObject == null)
            {
                _log.LogInformation($"{teamAbbreviation} boxscore not found. Exiting.");
                return null;
            }

            var scoreKey = SportToBoxscoreScoreKey[sport];
            var awayTeam = responseObject?["gameboxscore"]?["game"]?["awayTeam"]?["Abbreviation"].Value<string>();
            var homeTeam = responseObject?["gameboxscore"]?["game"]?["homeTeam"]?["Abbreviation"].Value<string>();
            var awayScore = responseObject?["gameboxscore"]?[$"{scoreKey}Summary"]?[$"{scoreKey}Totals"]?["awayScore"].Value<string>();
            var homeScore = responseObject?["gameboxscore"]?[$"{scoreKey}Summary"]?[$"{scoreKey}Totals"]?["homeScore"].Value<string>();
            return $"{teamAbbreviation} game is complete{Environment.NewLine}" +
                $"{awayTeam}: {awayScore}, {homeTeam}: {homeScore}{Environment.NewLine}" +
                $"{forDate.ToLongDateString()}";
        }

        #region GetGameStatus
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
            var responseObject = !string.IsNullOrWhiteSpace(response) ? JObject.Parse(response) : null;
            var games = responseObject?["scoreboard"]?["gameScore"];
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
        #endregion

        private static string GetFormattedDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMdd", new CultureInfo("en-US"));
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
