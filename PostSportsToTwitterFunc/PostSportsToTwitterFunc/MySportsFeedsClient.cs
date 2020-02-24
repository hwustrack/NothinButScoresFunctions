using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PostSportsToTwitterFunc
{
    public class MySportsFeedsClient
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

        public MySportsFeedsClient(ILogger log, HttpClient httpClient)
        {
            _log = log;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);
        }

        public async Task<string> GetGameStatusAsync(Sport sport, DateTime forDate, string teamAbbreviation)
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
            var teamKey = awayTeam == teamAbbreviation ? "awayTeam" : "homeTeam";
            var teamName = responseObject?["gameboxscore"]?["game"]?[teamKey]?["Name"].Value<string>();
            var awayScore = responseObject?["gameboxscore"]?[$"{scoreKey}Summary"]?[$"{scoreKey}Totals"]?["awayScore"].Value<string>();
            var homeScore = responseObject?["gameboxscore"]?[$"{scoreKey}Summary"]?[$"{scoreKey}Totals"]?["homeScore"].Value<string>();
            return $"{teamName} game is complete.{Environment.NewLine}" +
                $"{awayTeam}: {awayScore}, {homeTeam}: {homeScore}{Environment.NewLine}" +
                $"{forDate.ToLongDateString()}";
        }

        private static string GetFormattedDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMdd", new CultureInfo("en-US"));
        }
    }
}
