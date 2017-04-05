using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditBot
{
    class CapacitySizeException : Exception
    {
        public CapacitySizeException(string message) : base(message)
        {

        }
    }

    class IntervalSizeException : Exception
    {
        public IntervalSizeException(string message) : base(message)
        {

        }
    }

    class TokenBucket
    {
        public int currentTokens, capacity, interval;
        public DateTime lastRefreshed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="intervalInSeconds"></param>
        /// <exception cref="CapacitySizeException"></exception>
        /// <exception cref="IntervalSizeException"></exception>

        public TokenBucket(int capacity, int intervalInSeconds)
        {
            if (capacity < 1)
            {
                throw new CapacitySizeException("Capacity size must be greater than 0");
            }
            else if (intervalInSeconds < 1)
            {
                throw new IntervalSizeException("Interval size must be greater than 0");
            }
            this.currentTokens = capacity;
            this.capacity = capacity;
            this.interval = intervalInSeconds;
            this.lastRefreshed = DateTime.Now;
        }

        public bool RequestIsAllowed(int tokens)
        {
            Refill();
            if (currentTokens >= tokens)
            {
                currentTokens = currentTokens - tokens;
                return true;
            }
            return false;
        }

        public bool Refill()
        {
            if (DateTime.Now.Subtract(lastRefreshed).TotalSeconds >= interval)
            {
                currentTokens = capacity;
                lastRefreshed = DateTime.Now;
                return true;
            }
            return false;
        }
    }
}
