using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace PostSportsToTwitterFunc
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 0 * * * * ")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
