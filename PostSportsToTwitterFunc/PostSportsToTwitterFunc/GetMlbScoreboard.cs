using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetMlbScoreboard
    {
        [FunctionName("GetMlbScoreboard")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            using (MySportsFeedsClient sportsClient = new MySportsFeedsClient(log))
            {
                string gameStatus = await sportsClient.GetGameStatusAsync();

                TwitterClient twitterClient = new TwitterClient(log);
                twitterClient.PostTweetIfNotAlreadyPosted("qxnpop", gameStatus);
            }
        }
    }
}
