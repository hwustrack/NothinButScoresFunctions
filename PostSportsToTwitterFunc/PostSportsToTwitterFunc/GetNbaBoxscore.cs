using System;
using System.Collections.Generic;
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
        };

        [FunctionName("GetNbaBoxscore")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            using (MySportsFeedsClient sportsClient = new MySportsFeedsClient(log))
            {
                foreach (Team team in Teams)
                {
                    string gameStatus = await sportsClient.GetGameStatus2Async(Sport, ForDate, team.TeamAbbreviation);

                    TwitterClient twitterClient = new TwitterClient(log);
                    twitterClient.PostTweetIfNotAlreadyPosted(team.TwitterUser, gameStatus);
                }
            }
        }
    }
}
