using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditBot
{
    /// <summary>
    /// Exception is thrown if client information in Authenticate is invalid
    /// </summary>
    public class OAuthException : Exception
    {
        public OAuthException(string message) : base(message)
        {

        }
    }

    public class RedditBot
    {
        private string _name;
        private string _version;
        private string _userAgent;
        private string _redditUsername;
        private string _accessToken = "";
        private DateTime _accessTokenExpirationTime;
        private TokenBucket _tbucket = new TokenBucket(60, 60);
        private static HttpClient _client = new HttpClient();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the RedditBot</param>
        /// <param name="version">Bot Version</param>
        public RedditBot(string name, string version)
        {
            _name = name;
            _version = version;
        }
        
        /// <summary>
        /// Returns time left to access token expiration in seconds.
        /// </summary>
        private int AccessTokenExpirationInSeconds
        {
            get
            {
                return Convert.ToInt32((_accessTokenExpirationTime - DateTime.Now).TotalSeconds);
            }
        }

        /// <summary>
        /// Returns true if user is correctly authenticated via Authenticate(), else false
        /// </summary>
        /// <returns></returns>
        public bool IsAuthenticated()
        {
            if (_accessToken.Length == 0 || AccessTokenExpirationInSeconds <= 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Authenticate the bot to allow for OAuth requests.
        /// </summary>
        /// <param name="clientId">clientId of reddit account</param>
        /// <param name="clientSecret">clientSecret of reddit account</param>
        /// <param name="username">The reddit account's username</param>
        /// <param name="password">The reddit account's password</param>
        /// <exception cref="OAuthException"></exception>
        public void Authenticate(string clientId, string clientSecret, string username, string password)
        {
            if (!_tbucket.RequestIsAllowed())
            {
                _tbucket.Delay(_tbucket.TimeToNextRefillInSeconds());
            }
            _redditUsername = username;
            var authenticationArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            var encodedAuthenticationString = Convert.ToBase64String(authenticationArray);

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedAuthenticationString);
            _userAgent = $"{_name} /v{_version} by {username}";
            _client.DefaultRequestHeaders.Add("User-Agent", _userAgent);
               
            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", username },
                { "password", password }
            };
            var encodedFormData = new FormUrlEncodedContent(formData);

            var authUrl = "https://www.reddit.com/api/v1/access_token";
            var response = _client.PostAsync(authUrl, encodedFormData).GetAwaiter().GetResult();
            if (response.StatusCode.ToString() == "OK") {
                var responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine(responseData);
                var responseToken = JObject.Parse(responseData).SelectToken("access_token");
                if (responseToken == null) {
                    throw new OAuthException("Invalid client information");
                }
                // Actual Token
                _accessToken = responseToken.ToString();
                var expirationTimeInSeconds = Int32.Parse(JObject.Parse(responseData).SelectToken("expires_in").ToString());
                _accessTokenExpirationTime = DateTime.Now.AddSeconds(expirationTimeInSeconds);

                // Update AuthorizationHeader
                _client.DefaultRequestHeaders.Authorization = new
                System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);
            }
        }

        /// <summary>
        /// Returns a JObject containing the first 25 articles in a subreddit.
        /// </summary>
        /// <param name="subreddit">The name of the subreddit</param>
        /// <returns></returns>
        private JObject GetSubredditArticles(string subreddit)
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

        /// <summary>
        /// Returns a JArray containing article comments in JSON-structure.
        /// </summary>
        /// <param name="subreddit">The name of the subreddit</param>
        /// <param name="articleId">The id of the article</param>
        /// <returns></returns>
        private JArray GetArticleComments(string subreddit, string articleId)
        {
            if (!_tbucket.RequestIsAllowed())
            {
                _tbucket.Delay(_tbucket.TimeToNextRefillInSeconds());
            }
            else
            {
                Console.WriteLine("Request sent: getArticleComments");
            }
            string url = "https://oauth.reddit.com/r/" + subreddit + "/comments/" + articleId;

            var response = _client.GetStringAsync(url).GetAwaiter().GetResult();
            return JArray.Parse(response);
        }

        /// <summary>
        /// Returns true if a comment in a replytree is written by the reddit account, else false.
        /// </summary>
        /// <param name="comments">A JSON-list of comments</param>
        /// <returns></returns>
        private bool IsRepliedByBot(JToken comments)
        {
            if (comments.ToString() == "")
            {
                return false;
            }
            var items = comments["data"]["children"];
            foreach (var c in items)
            {
                var kind = c["kind"].ToString();
                if (kind == "more")
                {
                    return true;
                }

                var author = c["data"]["author"].ToString();
                if (author == _redditUsername)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Searches comments and replies for specified strings. If a specified string is found, the comment is replied.
        /// </summary>
        /// <param name="comments">A JSON-list of comments</param>
        /// <param name="searchStrings">A List of strings containing words to search for</param>
        private void SearchCommentsAndReplies(JToken comments, List<string> searchStrings)
        {
            var items = comments["data"]["children"];
            foreach (var c in items)
            {

                if (c["kind"].ToString() == "more")
                {
                    //Console.WriteLine("Is more");
                    return;
                }
                var text = c["data"]["body"].ToString();
                var author = c["data"]["author"].ToString();
                //Console.WriteLine($"{text}, {author}");

                if (author != _redditUsername)
                {
                    foreach (var word in searchStrings) {
                        if (text.Contains(word))
                        {
                            //Console.WriteLine("Contains Dexter");
                            var replies = c["data"]["replies"];
                            //Console.WriteLine(replies);
                            if (!IsRepliedByBot(replies))
                            {
                                //Console.WriteLine("Is not replied by bot");
                                ReplyToComment(c["data"]["name"].ToString(), "You said Dexter! // Nasir");
                            }
                            break;
                        }
                    }
                }
                if (c["data"]["replies"].ToString() != "")
                {
                    //Console.WriteLine("Replies empty");
                    SearchCommentsAndReplies(c["data"]["replies"], searchStrings);
                }
            }
        }

        /// <summary>
        /// Replies with a specified text to an article comment.
        /// </summary>
        /// <param name="commentId">The id of the comment</param>
        /// <param name="text">The text of the reply</param>
        private void ReplyToComment(string commentId, string text)
        {
            if (!_tbucket.RequestIsAllowed())
            {
                _tbucket.Delay(_tbucket.TimeToNextRefillInSeconds());
            }
            else
            {
                Console.WriteLine("Request sent: Replied to comment");
            }

            string url = "https://oauth.reddit.com/api/comment";
            var formData = new Dictionary<string, string>
            {
                { "api_type", "json" },
                { "text", text },
                { "thing_id", commentId }
            };
            var encodedFormData = new FormUrlEncodedContent(formData);

            var response = _client.PostAsync(url, encodedFormData).GetAwaiter().GetResult();
            //Console.WriteLine(response.IsSuccessStatusCode);
            //Console.WriteLine("Comment replied");
        }

        /// <summary>
        /// Runs the bot, searches a subreddit and its articles' comments for specified strings.
        /// Replies to the comment if a matched string is found.
        /// </summary>
        public void RunAndReply()
        {
            List<string> words = new List<string> { "Dexter", "dexter", "DEXTER" };
            //var articles = GetSubredditArticles("BotBois")["data"]["children"];
            ArticleHandler ah = new ArticleHandler(_tbucket, _client);
            CommentHandler ch = new CommentHandler(_tbucket, _client);
            var articles = ah.Fetch("BotBois")["data"]["children"];
            foreach (var article in articles)
            {
                var id = article["data"]["id"].ToString();
                //var comments = GetArticleComments("BotBois", id);
                var comments = ch.Fetch("BotBois", id);
                var items = comments[1];
                //SearchCommentsAndReplies(items, words);
                ch.SearchCommentsAndReplies(items, _redditUsername, words);
                System.Threading.Thread.Sleep(1000);
            }

            /*var id = "661zvu";
            var comments = GetArticleComments("sandboxtest", id);
            var items = comments[1];
            SearchCommentsAndReplies(items);*/
        }
    }
}

