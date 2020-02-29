using System;

namespace PostSportsToTwitterFunc
{
    public class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
