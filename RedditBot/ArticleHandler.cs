using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace RedditBot
{
    /// <summary>
    /// Class to handle reddit articles
    /// </summary>
    public class ArticleHandler
    {
        private TokenBucket _tbucket;
        private HttpClient _client;

        public ArticleHandler(TokenBucket tbucket, HttpClient authenticatedClient)
        {
            _tbucket = tbucket;
            _client = authenticatedClient;
        }

        /// <summary>
        /// Returns a JObject containing the first 25 articles in a subreddit.
        /// </summary>
        /// <param name="subreddit">The name of the subreddit</param>
        /// <returns></returns>
        public JObject Fetch(string subreddit)
        {
            if (!_tbucket.RequestIsAllowed())
            {
                _tbucket.Delay(_tbucket.TimeToNextRefillInSeconds());
            }
            else
            {
                Console.WriteLine("Request sent: getSubredditArticles");
            }
            string url = "https://oauth.reddit.com/r/" + subreddit;

            var response = _client.GetStringAsync(url).GetAwaiter().GetResult();
            return JObject.Parse(response);
        }
    }
}
