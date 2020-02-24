using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PostSportsToTwitterFunc
{
    public class EspnClient
    {
        public enum Sport
        {
            NBA
        };

        private const string FinalStatusName = "STATUS_FINAL";
        private const string QueryParameters = "?limit=100";
        private static readonly Uri BaseUri = new Uri("https://site.api.espn.com/apis/site/v2/sports/");
        private static readonly Dictionary<Sport, Uri> SportToScoreboardUri = new Dictionary<Sport, Uri>()
        {
            { Sport.NBA, new Uri(BaseUri, "basketball/nba/scoreboard") },
        };
        private static readonly TimeZoneInfo DisplayTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public EspnClient(ILogger log, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _log = log;
        }

        public async Task<string> GetGameStatusAsync(Sport sport, string teamAbbreviation)
        {
            Uri scoreboardUri = new Uri(BaseUri, $"{SportToScoreboardUri[sport]}{QueryParameters}");

            var response = await _httpClient.GetStringAsync(scoreboardUri);
            var gameStatus = GetGameStatusFromResponse(response, teamAbbreviation);

            return gameStatus;
        }

        private string GetGameStatusFromResponse(string response, string teamAbbreviation)
        {
            var responseObject = !string.IsNullOrWhiteSpace(response) ? JObject.Parse(response) : null;
            if (responseObject == null)
            {
                _log.LogInformation($"Could not parse response for {teamAbbreviation}. Exiting.");
                return null;
            }

            var ev = GetEventFromResponse(responseObject, teamAbbreviation);
            if (ev == null) return null;
            if (!IsGameComplete(ev)) return null;

            var competitors = ev["competitions"]?[0]?["competitors"];
            var awayTeam = competitors?.Children().Where(c => c["homeAway"].Value<string>() == "away").First();
            var homeTeam = competitors?.Children().Where(c => c["homeAway"].Value<string>() == "home").First();
            var awayTeamAbbreviation = awayTeam["team"]?["abbreviation"].Value<string>();
            var homeTeamAbbreviation = homeTeam["team"]?["abbreviation"].Value<string>();
            var team = awayTeamAbbreviation == teamAbbreviation ? awayTeam : homeTeam;
            var teamName = team["team"]?["name"].Value<string>();
            var awayScore = awayTeam["score"].Value<string>();
            var homeScore = homeTeam["score"].Value<string>();
            var date = ev["date"].Value<DateTime>().ToUniversalTime();
            var convertedDate = TimeZoneInfo.ConvertTimeFromUtc(date, DisplayTimeZone).ToLongDateString();
            return $"{teamName} game is complete.{Environment.NewLine}" +
                $"{awayTeamAbbreviation}: {awayScore}, {homeTeamAbbreviation}: {homeScore}{Environment.NewLine}" +
                $"{convertedDate}";
        }

        private static JToken GetEventFromResponse(JObject response, string teamAbbreviation)
        {
            return response["events"].Children()
                .Where(ev => ev["competitions"][0]["competitors"].Children()["team"]["abbreviation"]
                    .Any(abb => abb.Value<string>() == teamAbbreviation))
                .FirstOrDefault();
        }

        private static bool IsGameComplete(JToken ev)
        {
            return ev?["status"]?["type"]?["name"]?.Value<string>() == FinalStatusName;
        }
    }
}
