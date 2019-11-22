using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PostSportsToTwitterFunc
{
    public static class GetPgaTourLeaderboard
    {
        [FunctionName("GetPgaTourLeaderboard")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            using (PgaTourClient pgaTourClient = new PgaTourClient(log))
            {
                var formattedLeaderboard = await pgaTourClient.GetFormattedLeaderboardAsync();

                TwitterClient twitterClient = new TwitterClient(log);
                twitterClient.PostTweetIfNotAlreadyPosted("PGATourScores", formattedLeaderboard);
            }
        }
    }
}
