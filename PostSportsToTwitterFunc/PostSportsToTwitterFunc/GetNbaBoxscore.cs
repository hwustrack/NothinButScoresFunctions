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
        private static readonly List<Team> Teams = new List<Team>
        {
            new Team() { TeamAbbreviation = "MIL", TwitterUser = "BucksScores" },
            new Team() { TeamAbbreviation = "CHI", TwitterUser = "Bulls_Scores" },
            new Team() { TeamAbbreviation = "WSH", TwitterUser = "Wizards_Scores" },
            new Team() { TeamAbbreviation = "LAL", TwitterUser = "Lakers_Scores" }
        };

        [FunctionName("GetNbaBoxscore")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                EspnClient espnClient = new EspnClient(log, new Clock(), httpClient);
                await espnClient.SetGameStatusesAsync(EspnClient.Sport.NBA, Teams);

                TwitterClient twitter = new TwitterClient(log);
                foreach (var team in Teams)
                {
                    twitter.PostTweetIfNotAlreadyPosted(team.TwitterUser, team.Status);
                }
            }
        }
    }
}
