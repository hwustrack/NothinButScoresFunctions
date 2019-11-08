using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetNbaBoxscore
    {
        private const MySportsFeedsClient.Sport Sport = MySportsFeedsClient.Sport.NBA;
        private const string TeamAbbreviation = "MIL";
        private static readonly DateTime ForDate = DateTime.UtcNow - TimeSpan.FromHours(7); // pacific hours so there's time for the game to be finalized

        [FunctionName("GetNbaBoxscore")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            using (MySportsFeedsClient sportsClient = new MySportsFeedsClient(log))
            {
                string gameStatus = await sportsClient.GetGameStatus2Async(Sport, ForDate, TeamAbbreviation);

                TwitterClient twitterClient = new TwitterClient(log);
                twitterClient.PostTweetIfNotAlreadyPosted("qxnpop", gameStatus);
            }
        }
    }
}
