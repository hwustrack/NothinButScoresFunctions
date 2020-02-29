using System;
using System.Collections.Generic;
using System.Text;
using PostSportsToTwitterFunc;

namespace PostSportsToTwitterFuncTests
{
    internal class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; }

        public DateTimeOffset Now { get; set; }
    }
}
