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
    public class Program
    {
        static void Main(string[] args)
        {
            var clientId = ConfigurationManager.AppSettings["clientId"];
            var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            RedditBot bot = new RedditBot("UltimateBottyBoi", "1.0");

            bot.Authenticate(clientId, clientSecret, username, password);
            Console.WriteLine($"Is authenticated: {bot.IsAuthenticated()}");
            while (true)
            {
                bot.RunAndReply();
            }
            //Console.ReadKey();
        }
    }
}
