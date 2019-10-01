using System;
using System.Linq;
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

        public void PostTweet(string content)
        {
            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
            Tweet.PublishTweet(content);
        }

        public bool FindTweet(string user, string content)
        {
            Auth.SetApplicationOnlyCredentials(ConsumerKey, ConsumerSecret, initializeBearerToken: true);

            var searchParameters = new SearchTweetsParameters($"from:{user} \"{content}\"");
            var tweets = Search.SearchTweets(searchParameters);
            return tweets.Any();
        }
    }
}
