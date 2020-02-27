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

        public async Task<Dictionary<string, string>> GetGameStatusesAsync(Sport sport, List<string> teamAbbreviations)
        {
            Uri scoreboardUri = new Uri(BaseUri, $"{SportToScoreboardUri[sport]}{QueryParameters}");

            _ = await _httpClient.GetStringAsync(scoreboardUri); // for some reason the first request gets stale data
            var response = await _httpClient.GetStringAsync(scoreboardUri);
            var gameStatuses = GetGameStatusesFromResponse(response, teamAbbreviations);

            return gameStatuses;
        }

        private Dictionary<string, string> GetGameStatusesFromResponse(string response, List<string> teamAbbreviations)
        {
            var responseObject = !string.IsNullOrWhiteSpace(response) ? JObject.Parse(response) : null;
            if (responseObject == null)
            {
                _log.LogError($"Could not parse response for {teamAbbreviations}. Exiting.");
                return null;
            }

            Dictionary<string, string> statuses = new Dictionary<string, string>();
            foreach (var teamAbbreviation in teamAbbreviations)
            {
                var ev = GetEventFromResponse(responseObject, teamAbbreviation);
                if (ev == null)
                {
                    _log.LogInformation($"{teamAbbreviation} game not found. Skipping.");
                    continue;
                }
                var gameStatus = ev["status"]["type"]["name"].Value<string>();
                var displayClock = ev["status"]["displayClock"].Value<string>();
                var period = ev["status"]["period"].Value<string>();
                if (gameStatus != FinalStatusName)
                {
                    _log.LogInformation($"{teamAbbreviation} game is {gameStatus}, period: {period}, displayClock: {displayClock}. Not complete. Skipping.");
                    continue;
                }

                var competitors = ev["competitions"][0]["competitors"];
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
                var status = $"{teamName} game is complete.{Environment.NewLine}" +
                    $"{awayTeamAbbreviation}: {awayScore}, {homeTeamAbbreviation}: {homeScore}{Environment.NewLine}" +
                    $"{convertedDate}";
                statuses.Add(teamAbbreviation, status);
            }
            return statuses;
        }

        private static JToken GetEventFromResponse(JObject response, string teamAbbreviation)
        {
            return response["events"].Children()
                .Where(ev => ev["competitions"][0]["competitors"].Children()["team"]["abbreviation"]
                    .Any(abb => abb.Value<string>() == teamAbbreviation))
                .FirstOrDefault();
        }
    }
}
