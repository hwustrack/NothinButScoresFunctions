using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PostSportsToTwitterFunc
{
    public sealed class PgaTourClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public PgaTourClient(ILogger log)
        {
            _httpClient = new HttpClient();
            _log = log;
        }

        public async Task<string> GetFormattedLeaderboardAsync()
        {
            var currentTournamentId = await GetCurrentTournamentIdAsync();
            Uri GetLeaderboardUri = new Uri($"https://statdata.pgatour.com/r/{currentTournamentId}/leaderboard-v2mini.json");

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

        private static bool IsRoundComplete(JObject response)
        {
            var distinctValues = response["leaderboard"]["players"].Children()["thru"].Values<int?>().Distinct();
            return distinctValues.Count() <= 2 && distinctValues.Contains(18) && distinctValues.Contains(null); // cut players have null
        }

        private static string FormatLeaderboard(JObject response)
        {
            string formattedResponse = string.Empty;
            int currentRound;
            string tournamentName;
            string currentPosition;
            string playerName;
            int currentScore;

            currentRound = response["leaderboard"]["current_round"].Value<int>();
            tournamentName = response["leaderboard"]["tournament_name"].Value<string>();
            formattedResponse += $"Round {currentRound} of the {tournamentName} is complete.{Environment.NewLine}{Environment.NewLine}";

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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
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
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
