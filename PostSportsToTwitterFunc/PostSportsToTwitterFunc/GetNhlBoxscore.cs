using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetNhlBoxscore
    {
        private const MySportsFeedsClient.Sport Sport = MySportsFeedsClient.Sport.NHL;
        private const string TeamAbbreviation = "STL";
        private static readonly DateTime ForDate = DateTime.UtcNow - TimeSpan.FromHours(12); // give a longer window for game to be finalized in sports api

        [FunctionName("GetNhlBoxscore")]
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
