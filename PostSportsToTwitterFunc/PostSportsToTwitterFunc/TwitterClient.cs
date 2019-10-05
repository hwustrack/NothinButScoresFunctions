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
        private static readonly string AccessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
        private static readonly string AccessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");

        private readonly ILogger _log;

        public TwitterClient(ILogger log)
        {
            _log = log;
        }

        public void PostTweetIfNotAlreadyPosted(string user, string content)
        {
            if (!FindTweet(user, content))
            {
                PostTweet(content);
            }
            else
            {
                _log.LogInformation("Tweet already posted. Exiting.");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public void PostTweet(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
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
