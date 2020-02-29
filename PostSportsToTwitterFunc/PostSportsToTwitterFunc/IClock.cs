using System;

namespace PostSportsToTwitterFunc
{
    public interface IClock
    {
        DateTime UtcNow { get; }
        DateTimeOffset Now { get; }
    }
}
