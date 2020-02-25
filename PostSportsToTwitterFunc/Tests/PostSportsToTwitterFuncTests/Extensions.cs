using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;

namespace PostSportsToTwitterFuncTests
{
    public static class Extensions
    {
        public static void VerifyLogged(this Mock<ILogger> mockLogger, Times times)
        {
            mockLogger.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), 
                It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), times);
        }
    }
}
