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
    class OAuthException : Exception
    {
        public OAuthException(string message) : base(message)
        {

        }
    }

    class AccessTokenExpiredException : Exception
    {
        public AccessTokenExpiredException(string message) : base(message)
        {

        }
    }

    class RedditBot
    {
        private string _name;
        private string _description;
        private string _version;
        private string _userAgent;
        private string _accessToken = "";
        private DateTime _accessTokenExpirationTime;
        private TokenBucket _tbucket;

        public RedditBot(string name, string description, string version, TokenBucket tbucket)
        {
            _name = name;
            _description = description;
            _version = version;
            _tbucket = tbucket;
        }

        public int AccessTokenExpirationInSeconds
        {
            get
            {
                return Convert.ToInt32((_accessTokenExpirationTime - DateTime.Now).TotalSeconds);
            }
        }

        public bool IsAuthenticated()
        {
            if (_accessToken.Length == 0 || AccessTokenExpirationInSeconds <= 0)
            {
                return false;
            }
            return true;
        }

        public void Authenticate(string clientId, string clientSecret, string username, string password)
        {
            using (var client = new HttpClient())
            {
                var authenticationArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
                var encodedAuthenticationString = Convert.ToBase64String(authenticationArray);

                client.DefaultRequestHeaders.Authorization = new
                System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedAuthenticationString);
                _userAgent = $"{_name} /v{_version} by {username}";
                client.DefaultRequestHeaders.Add("User-Agent", _userAgent);
               
                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", username },
                    { "password", password }
                };
                var encodedFormData = new FormUrlEncodedContent(formData);

                var authUrl = "https://www.reddit.com/api/v1/access_token";
                var response = client.PostAsync(authUrl, encodedFormData).GetAwaiter().GetResult();

                if (response.StatusCode.ToString() == "OK") {
                    var responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var responseToken = JObject.Parse(responseData).SelectToken("access_token");
                    if (responseToken == null) {
                        throw new OAuthException("Invalid client information");
                    }
                    // Actual Token
                    _accessToken = responseToken.ToString();
                    var expirationTimeInSeconds = Int32.Parse(JObject.Parse(responseData).SelectToken("expires_in").ToString());
                    _accessTokenExpirationTime = DateTime.Now.AddSeconds(expirationTimeInSeconds);
                }

                /*

                // Base URL: https://oauth.reddit.com/api/v1/
                var requestUrl = "https://oauth.reddit.com/api/v1/me";

                // Update AuthorizationHeader
                client.DefaultRequestHeaders.Authorization = new
                System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);

                var meResponse = client.GetStringAsync(requestUrl).GetAwaiter().GetResult();
                Console.WriteLine(meResponse);

                */
            }
        }

        public JObject GetSubredditArticles(string subreddit)
        {
            using (var client = new HttpClient())
            {
                string url = "https://oauth.reddit.com/r/" + subreddit;

                // Update AuthorizationHeader
                client.DefaultRequestHeaders.Authorization = new
                System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);

                // User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", _userAgent);

                var response = client.GetStringAsync(url).GetAwaiter().GetResult();
                return JObject.Parse(response);
            }
        }

        public JArray GetArticleComments(string subreddit, string articleId)
        {
            using (var client = new HttpClient())
            {
                string url = "https://oauth.reddit.com/r/" + subreddit + "/comments/" + articleId;

                // Update AuthorizationHeader
                client.DefaultRequestHeaders.Authorization = new
                System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);

                // User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", _userAgent);

                var response = client.GetStringAsync(url).GetAwaiter().GetResult();
                return JArray.Parse(response);
            }
        }

        public bool IsRepliedByBot(JToken comments)
        {
            if (comments.ToString() == "")
            {
                return false;
            }
            var items = comments["data"]["children"];
            foreach (var c in items)
            {
                var body = c["data"]["body"].ToString();
                var author = c["data"]["author"].ToString();
                if (author == "botboi")
                {
                    return true;
                }
            }
            return false;
        }

        public void SearchCommentsAndReplies(JToken comments)
        {
            var items = comments["data"]["children"];
            foreach (var c in items)
            {
                if (c["kind"].ToString() == "more")
                {
                    return;
                }
                var text = c["data"]["body"].ToString();
                var author = c["data"]["author"].ToString();
                if (author != "botboi")
                {
                    if (text.Contains("Dexter") || text.Contains("dexter"))
                    {
                        if (!_tbucket.RequestIsAllowed(1))
                        {
                            Console.WriteLine("Request not allowed");
                        }
                        else
                        {
                            var replies = c["data"]["replies"];
                            if (!IsRepliedByBot(replies))
                            {
                                ReplyToComment(c["data"]["name"].ToString(), "You said Dexter!");
                                Console.WriteLine("Request sent: replied");
                            }

                        }
                    }
                }
                if (c["data"]["replies"].ToString() != "")
                {
                    SearchCommentsAndReplies(c["data"]["replies"]);
                }
            }
        }

        public void ReplyToComment(string commentId, string text)
        {
            using (var client = new HttpClient())
            {
                string url = "https://oauth.reddit.com/api/comment";

                var formData = new Dictionary<string, string>
                {
                    { "api_type", "json" },
                    { "text", text },
                    { "thing_id", commentId }
                };
                var encodedFormData = new FormUrlEncodedContent(formData);

                // Update AuthorizationHeader
                client.DefaultRequestHeaders.Authorization = new
                System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);

                // User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", _userAgent);

                var response = client.PostAsync(url, encodedFormData).GetAwaiter().GetResult();
                var responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine(response.StatusCode.ToString());
                //Console.WriteLine(responseData);
            }
        }

        public void Hello()
        {
            var articles = GetSubredditArticles("sandboxtest")["data"]["children"];
            Console.WriteLine("Request sent: getSubbredditArticles");
            foreach (var article in articles)
            {
                var id = article["data"]["id"].ToString();
                var comments = GetArticleComments("sandboxtest", id);
                Console.WriteLine("Request sent: getArticleComments");
                var items = comments[1];
                SearchCommentsAndReplies(items);
            }
            /*var id = "61v0tm";
            var comments = GetArticleComments("sandboxtest", id);
            var items = comments[1];
            SearchCommentsAndReplies(items);*/
        }

        public void CommentHello()
        {
            if (!_tbucket.RequestIsAllowed(3))
            {
                Console.WriteLine("Request not allowed");
                return;
            }

            var subreddits = GetSubredditArticles("sandboxtest").SelectToken("data").SelectToken("children");

            foreach (var article in subreddits)
            {
                var id = article.SelectToken("data.id").ToString();
                var comments = GetArticleComments("sandboxtest", id);
                var items = comments[1]["data"]["children"];
                foreach (var i in items)
                {
                    var linkId = i.SelectToken("data").SelectToken("name").ToString();
                    var body = i.SelectToken("data").SelectToken("body").ToString();
                    if (body.Contains("Dexter") || body.Contains("dexter"))
                    {
                        if (i.SelectToken("data").SelectToken("replies").ToString().Length == 0)
                        {
                            ReplyToComment(linkId, "Hello Dexter aka Morot!");
                        }
                    }
                }
            }
        }
    }
}
