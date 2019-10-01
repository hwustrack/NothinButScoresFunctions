using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetMlbScoreboard
    {
        [FunctionName("GetMlbScoreboard")]
        public static async Task Run([TimerTrigger("0 0 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            MySportsFeedsClient sportsClient = new MySportsFeedsClient(log);
            string gameStatus = await sportsClient.GetGameStatusAsync();

            TwitterClient twitterClient = new TwitterClient();
            if (!twitterClient.FindTweet("qxnpop", gameStatus))
            {
                twitterClient.PostTweet(gameStatus);
            }
            else
            {
                log.LogDebug("Tweet already posted. Exiting.");
            }
        }
    }
}
