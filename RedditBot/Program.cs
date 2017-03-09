using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Configuration;

namespace RedditBot
{
    class Program
    {
        static void Main(string[] args)
        {
            RedditBot bot = new RedditBot("UltimateBottyBoi", "A very good bot", "1.0");
            var clientId = ConfigurationManager.AppSettings["clientId"];
            var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            Console.WriteLine(bot.isAuthenticated());
            bot.Authenticate(clientId, clientSecret, username, password);
            Console.WriteLine(bot.isAuthenticated());
        }
    }
}
