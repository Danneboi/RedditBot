using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditBot
{
    /// <summary>
    /// Exception is thrown if capacity size is below 1.
    /// </summary>
    public class CapacitySizeException : Exception
    {
        /// <summary>
        /// Constructur
        /// </summary>
        /// <param name="message">Message to display with exception</param>
        public CapacitySizeException(string message) : base(message)
        {

        }
    }

    /// <summary>
    /// Exception is thrown if interval is below 1.
    /// </summary>
    public class IntervalSizeException : Exception
    {
        public IntervalSizeException(string message) : base(message)
        {

        }
    }

    public class TokenBucket
    {
        private int _currentTokens, _capacity, _interval;
        private DateTime _lastRefreshed;

        /// <summary>
        /// Constructor for TokenBucket.
        /// </summary>
        /// <param name="capacity">Integer capacity for the token bucket</param>
        /// <param name="intervalInSeconds">Interval in seconds for refilling the token bucket</param>
        /// <exception cref="CapacitySizeException">If capacity is below 1</exception>
        /// <exception cref="IntervalSizeException">If interval is below 1</exception>

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
            _currentTokens = capacity;
            _capacity = capacity;
            _interval = intervalInSeconds;
            _lastRefreshed = DateTime.Now;
        }

        /// <summary>
        /// Returns time to next refill in seconds. If time has been passed, the method returns 0.
        /// </summary>
        /// <returns>A double, time in seconds</returns>
        public int TimeToNextRefillInSeconds()
        {
            double time = _interval - DateTime.Now.Subtract(_lastRefreshed).TotalSeconds;
            if (time <= 0)
            {
                return 0;
            }
            return (int)(Math.Ceiling(time));
        }
    
        /// <summary>
        /// Delays the thread to wait for a request to be completed.
        /// </summary>
        /// <param name="delayInSeconds">Time to delay in seconds</param>
        public void Delay(int delayInSeconds)
        {
            Console.WriteLine($"Delaying for {delayInSeconds} seconds");
            System.Threading.Thread.Sleep(delayInSeconds * 1000);
        }

        /// <summary>
        /// Returns true if request is allowed, else false.
        /// While checking if the request is allowed, the bucket is refilled if the interval has been passed.
        /// Default amount of tokens is 1.
        /// </summary>
        /// <param name="tokens">The amount of tokens needed for the request(s)</param>
        /// <returns>bool, true if allowed else false</returns>
        public bool RequestIsAllowed(int tokens=1)
        {
            Refill();
            if (_currentTokens >= tokens)
            {
                _currentTokens = _currentTokens - tokens;
                return true;
            }
            return false;
        }

        /// <summary>
        /// If interval has been passed, the bucket is refilled with the initial capacity.
        /// </summary>
        /// <returns>bool, true if bucket is refilled, else false.</returns>
        private bool Refill()
        {
            if (DateTime.Now.Subtract(_lastRefreshed).TotalSeconds >= _interval)
            {
                _currentTokens = _capacity;
                _lastRefreshed = DateTime.Now;
                return true;
            }
            return false;
        }
    }
}
