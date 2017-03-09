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
    class RedditBot
    {
        private string _name;
        private string _description;
        private string _version;
        private string _accessToken = "";

        public RedditBot(string name, string description, string version)
        {
            _name = name;
            _description = description;
            _version = version;
        }

        public bool IsAuthenticated()
        {
            if (_accessToken.Length == 0)
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

                client.DefaultRequestHeaders.Add("User-Agent", $"{_name} /v{_version} by {username}");

                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", username },
                    { "password", password }
                };
                var encodedFormData = new FormUrlEncodedContent(formData);

                var authUrl = "https://www.reddit.com/api/v1/access_token";
                var response = client.PostAsync(authUrl, encodedFormData).GetAwaiter().GetResult();

                // Response Code
                // Console.WriteLine(response.StatusCode);

                if (response.StatusCode.ToString() == "OK") {
                    // Actual Token
                    var responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    _accessToken = JObject.Parse(responseData).SelectToken("access_token").ToString();
                }

                // Update AuthorizationHeader
                //client.DefaultRequestHeaders.Authorization = new
                //System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);
            }
        }
    }
}
