﻿using System;
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
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            using (PgaTourClient pgaTourClient = new PgaTourClient())
            {
                var formattedLeaderboard = await pgaTourClient.GetFormattedLeaderboardAsync();

                TwitterClient twitterClient = new TwitterClient(log);
                twitterClient.PostTweetIfNotAlreadyPosted("qxnpop", formattedLeaderboard);
            }
        }
    }
}
