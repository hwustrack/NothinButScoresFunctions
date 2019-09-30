using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Tweetinvi;

namespace PostSportsToTwitterFunc
{
    public class TwitterClient
    {
        private static readonly string ConsumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
        private static readonly string ConsumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");
        private static readonly string AccessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
        private static readonly string AccessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");

        public TwitterClient()
        {
            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
        }

        public void PostTweet(string content)
        {
            Tweet.PublishTweet(content);
        }
    }
}
