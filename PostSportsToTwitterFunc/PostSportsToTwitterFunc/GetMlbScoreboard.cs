using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetMlbScoreboard
    {
        private const MySportsFeedsClient.Sport Sport = MySportsFeedsClient.Sport.MLB;
        private const string TeamAbbreviation = "MIL";
        private static readonly DateTime ForDate = DateTime.Today - TimeSpan.FromDays(60);

        [FunctionName("GetMlbScoreboard")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            using (MySportsFeedsClient sportsClient = new MySportsFeedsClient(log))
            {
                string gameStatus = await sportsClient.GetGameStatusAsync(Sport, ForDate, TeamAbbreviation);

                TwitterClient twitterClient = new TwitterClient(log);
                twitterClient.PostTweetIfNotAlreadyPosted("qxnpop", gameStatus);
            }
        }
    }
}
