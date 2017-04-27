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
    /// Class to handle reddit comment searches and replies
    /// </summary>
    public class CommentHandler
    {
        private TokenBucket _tbucket;
        private HttpClient _client;

        public CommentHandler(TokenBucket tbucket, HttpClient authenticatedClient)
        {
            _tbucket = tbucket;
            _client = authenticatedClient;
        }

        /// <summary>
        /// Returns a JArray containing article comments in JSON-structure.
        /// </summary>
        /// <param name="subreddit">The name of the subreddit</param>
        /// <param name="articleId">The id of the article</param>
        /// <returns></returns>
        public JArray Fetch(string subreddit, string articleId)
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
        private bool IsRepliedByBot(JToken comments, string botUsername)
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
                if (author == botUsername)
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
        /// <param name="botUsername">The bot's username on reddit</param>
        /// <param name="searchStrings">A List of strings containing words to search for</param>
        public void SearchCommentsAndReplies(JToken comments, string botUsername, List<string> searchStrings)
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

                if (author != botUsername)
                {
                    foreach (var word in searchStrings)
                    {
                        if (text.Contains(word))
                        {
                            //Console.WriteLine("Contains Dexter");
                            var replies = c["data"]["replies"];
                            //Console.WriteLine(replies);
                            if (!IsRepliedByBot(replies, botUsername))
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
                    SearchCommentsAndReplies(c["data"]["replies"], botUsername, searchStrings);
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
    }
}
