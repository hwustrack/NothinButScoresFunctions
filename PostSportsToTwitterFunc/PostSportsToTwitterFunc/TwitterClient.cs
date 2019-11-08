using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Parameters;

namespace PostSportsToTwitterFunc
{
    public class TwitterClient
    {
        private static readonly string ConsumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
        private static readonly string ConsumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");

        private readonly ILogger _log;

        public TwitterClient(ILogger log)
        {
            _log = log;
        }

        public void PostTweetIfNotAlreadyPosted(string user, string content)
        {
            if (!FindTweet(user, content))
            {
                PostTweet(user, content);
            }
            else
            {
                _log.LogInformation("Tweet already posted. Exiting.");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public void PostTweet(string user, string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            var accessToken = Environment.GetEnvironmentVariable($"TwitterAccessToken:{user}");
            var accessTokenSecret = Environment.GetEnvironmentVariable($"TwitterAccessTokenSecret:{user}");
            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, accessToken, accessTokenSecret);
            Tweet.PublishTweet(content);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public bool FindTweet(string user, string content)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(content)) return false;

            Auth.SetApplicationOnlyCredentials(ConsumerKey, ConsumerSecret, initializeBearerToken: true);

            var searchParameters = new SearchTweetsParameters($"from:{user} \"{content}\"");
            var tweets = Search.SearchTweets(searchParameters);
            return tweets != null ? tweets.Any() : false;
        }
    }
}
