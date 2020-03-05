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

        private readonly ILogger _log;
        private readonly IClock _clock;
        private readonly HttpClient _httpClient;

        public EspnClient(ILogger log, IClock clock, HttpClient httpClient)
        {
            _log = log;
            _clock = clock;
            _httpClient = httpClient;
        }

        public async Task SetGameStatusesAsync(Sport sport, List<Team> teams)
        {
            Uri scoreboardUri = new Uri(BaseUri, $"{SportToScoreboardUri[sport]}{QueryParameters}&{GetDateParameters()}");

            var response = await _httpClient.GetStringAsync(scoreboardUri);
            SetGameStatusesFromResponse(response, teams);

            return;
        }

        private void SetGameStatusesFromResponse(string response, List<Team> teams)
        {
            var responseObject = !string.IsNullOrWhiteSpace(response) ? JObject.Parse(response) : null;
            if (responseObject == null)
            {
                _log.LogError($"Could not parse response. Exiting.");
                return;
            }

            foreach (var team in teams)
            {
                var ev = GetEventFromResponse(responseObject, team.TeamAbbreviation);
                if (ev == null)
                {
                    _log.LogInformation($"{team.TeamAbbreviation} game not found. Skipping.");
                    continue;
                }
                var gameStatus = ev["status"]["type"]["name"].Value<string>();
                var displayClock = ev["status"]["displayClock"].Value<string>();
                var period = ev["status"]["period"].Value<string>();
                if (gameStatus != FinalStatusName)
                {
                    _log.LogInformation($"{team.TeamAbbreviation} game is {gameStatus}, period: {period}, displayClock: {displayClock}. Not complete. Skipping.");
                    continue;
                }

                var competitors = ev["competitions"][0]["competitors"];
                var awayTeam = competitors?.Children().Where(c => c["homeAway"].Value<string>() == "away").First();
                var homeTeam = competitors?.Children().Where(c => c["homeAway"].Value<string>() == "home").First();
                var awayTeamAbbreviation = awayTeam["team"]?["abbreviation"].Value<string>();
                var homeTeamAbbreviation = homeTeam["team"]?["abbreviation"].Value<string>();
                var thisTeam = awayTeamAbbreviation == team.TeamAbbreviation ? awayTeam : homeTeam;
                var teamName = thisTeam["team"]?["name"].Value<string>();
                var awayScore = awayTeam["score"].Value<string>();
                var homeScore = homeTeam["score"].Value<string>();
                var date = ev["date"].Value<DateTime>().ToUniversalTime();
                var convertedDate = TimeZoneInfo.ConvertTimeFromUtc(date, DisplayTimeZone).ToLongDateString();
                var status = $"{teamName} game is complete.{Environment.NewLine}" +
                    $"{awayTeamAbbreviation}: {awayScore}, {homeTeamAbbreviation}: {homeScore}{Environment.NewLine}" +
                    $"{convertedDate}";
                team.Status = status;
            }
        }

        private static JToken GetEventFromResponse(JObject response, string teamAbbreviation)
        {
            return response["events"].Children()
                .Where(ev => ev["competitions"][0]["competitors"].Children()["team"]["abbreviation"]
                    .Any(abb => abb.Value<string>() == teamAbbreviation))
                .FirstOrDefault();
        }

        private string GetDateParameters()
        {
            var pst = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            var pstDate = pst.ToString("yyyyMMdd");
            var epoch = _clock.Now.ToUnixTimeSeconds();
            return $"dates={pstDate}&{epoch}";
        }
    }
}
