using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jint;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PostSportsToTwitterFunc
{
    public sealed class PgaTourClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public PgaTourClient(ILogger log, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _log = log;
        }

        public async Task<string> GetFormattedLeaderboardAsync()
        {
            var currentTournamentId = await GetCurrentTournamentIdAsync();
            Uri GetLeaderboardUri = await GetLeaderboardUriAsync(currentTournamentId);

            var response = await _httpClient.GetStringAsync(GetLeaderboardUri);
            var responseObject = JObject.Parse(response);
            if (IsRoundComplete(responseObject))
            {
                return FormatLeaderboard(responseObject);
            }
            else
            {
                _log.LogInformation("Current round not complete. Exiting.");
                return null;
            }
        }

        private async Task<string> GetCurrentTournamentIdAsync()
        {
            Uri GetCurrentTournamentUri = new Uri("https://statdata.pgatour.com/r/current/message.json");

            var response = await _httpClient.GetStringAsync(GetCurrentTournamentUri);
            var responseObject = JObject.Parse(response);
            return responseObject["tid"].ToString();
        }

        private async Task<Uri> GetLeaderboardUriAsync(string currentTournamentId)
        {
            /*
             * In the browser, https://lbdata.pgatour.com/react-lb-js/stroke-play-leaderboard-controller-bcfd63a03ae332cc1919.js:formatted initiates the call to 
             * https://lbdata.pgatour.com/2020/r/004/leaderboard.json?userTrackingId=exp=1579891271~acl=*~hmac=d2f50fe2d0be8f5fc651ce3849f8180b753e925f1ea554066d5b3a580419f2a7.
             * It has a function, getUrlWithAuth, which adds the userTrackindId to the leaderboard url.
             * 
             * In getUrlWithAuth, i.UserIdTracker.getUserId() gets a hardcoded user id that appears to never change, id8730931. This user id is given as a paremeter
             * to https://microservice.pgatour.com/js which will output the string needed (exp=1894...). This js file seems to be constantly changing, which is what 
             * allows the userTrackingId to change with time.
             * 
             * The implementation gets the js file, and runs the magic static user id through that function which outputs the userTrackingId.
             * 
             */

            Uri GetUserTrackingIdFunctionUri = new Uri("https://microservice.pgatour.com/js");

            var response = await _httpClient.GetStringAsync(GetUserTrackingIdFunctionUri);

            var engine = new Engine();
            var js = response.Replace(@"(window.pgatour || (window.pgatour = {}))", "a", StringComparison.OrdinalIgnoreCase);
            js = "var a = {};" + js;
            js = js + @"t = a.setTrackingUserId; t(""id8730931"");";
            engine.Execute(js);
            var userTrackingId = engine.GetCompletionValue();

            return new Uri($"https://statdata.pgatour.com/r/{currentTournamentId}/leaderboard-v2mini.json" + $"?userTrackingId={userTrackingId}");
        }

        private static bool IsRoundComplete(JObject response)
        {
            var distinctValues = response["leaderboard"]["players"].Children()["thru"].Values<int?>().Distinct();
            return distinctValues.Count() == 1 && distinctValues.Contains(18) ||
                distinctValues.Count() == 2 && distinctValues.Contains(18) && distinctValues.Contains(null); // cut players have null
        }

        private static string FormatLeaderboard(JObject response)
        {
            string formattedResponse = string.Empty;
            int currentRound, totalRounds, currentScore;
            string tournamentName, roundName, currentPosition, playerName;

            currentRound = response["leaderboard"]["current_round"].Value<int>();
            totalRounds = response["leaderboard"]["total_rounds"].Value<int>();
            roundName = currentRound == totalRounds ? "Final round" : $"Round {currentRound}";
            tournamentName = response["leaderboard"]["tournament_name"].Value<string>();
            formattedResponse += $"{roundName} of the {tournamentName} is complete.{Environment.NewLine}{Environment.NewLine}";

            for (int i = 0; i < 5; i++)
            {
                currentPosition = response["leaderboard"]["players"][i]["current_position"].Value<string>();
                currentScore = response["leaderboard"]["players"][i]["total"].Value<int>();
                playerName = response["leaderboard"]["players"][i]["player_bio"]["first_name"].Value<string>();
                playerName += " " + response["leaderboard"]["players"][i]["player_bio"]["last_name"].Value<string>();
                formattedResponse += $"{currentPosition}. {playerName} {currentScore}{Environment.NewLine}";
            }

            return formattedResponse;
        }
    }
}
