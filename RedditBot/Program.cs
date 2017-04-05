using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Configuration;
using System.Threading;

namespace RedditBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientId = ConfigurationManager.AppSettings["clientId"];
            var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            TokenBucket tbucket = new TokenBucket(60, 60);
            RedditBot bot = new RedditBot("UltimateBottyBoi", "A very good bot", "1.0", tbucket);

            bot.Authenticate(clientId, clientSecret, username, password);
            Console.WriteLine($"Is authenticated: {bot.IsAuthenticated()}");

            bot.Hello();
            Console.ReadKey();
        }
    }
}
