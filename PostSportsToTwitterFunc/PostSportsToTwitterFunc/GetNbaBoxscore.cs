using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetNbaBoxscore
    {
        private const MySportsFeedsClient.Sport Sport = MySportsFeedsClient.Sport.NBA;
        private static readonly DateTime ForDate = DateTime.UtcNow - TimeSpan.FromHours(12); // give a longer window for game to be finalized in sports api
        private static readonly List<Team> Teams = new List<Team>
        {
            new Team() { TeamAbbreviation = "MIL", TwitterUser = "BucksScores" },
            new Team() { TeamAbbreviation = "CHI", TwitterUser = "Bulls_Scores" },
            new Team() { TeamAbbreviation = "WAS", TwitterUser = "Wizards_Scores"}
        };

        [FunctionName("GetNbaBoxscore")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                MySportsFeedsClient sportsClient = new MySportsFeedsClient(log, httpClient);

                foreach (Team team in Teams)
                {
                    string gameStatus = await sportsClient.GetGameStatusAsync(Sport, ForDate, team.TeamAbbreviation);

                    TwitterClient twitterClient = new TwitterClient(log);
                    twitterClient.PostTweetIfNotAlreadyPosted(team.TwitterUser, gameStatus);
                }
            }

            using (HttpClient httpClient = new HttpClient())
            {
                EspnClient espnClient = new EspnClient(log, httpClient);
                var statuses = await espnClient.GetGameStatusesAsync(EspnClient.Sport.NBA, new List<string>() { "MIL", "CHI" });

                TwitterClient twitter = new TwitterClient(log);
                foreach (var status in statuses.Values)
                {
                    twitter.PostTweetIfNotAlreadyPosted("qxnpop", status);
                }
            }
        }
    }
}
