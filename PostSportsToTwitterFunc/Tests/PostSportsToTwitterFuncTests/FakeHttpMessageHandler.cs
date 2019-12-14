using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PostSportsToTwitterFuncTests
{
    internal class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>();
        private readonly List<HttpRequestMessage> _requests = new List<HttpRequestMessage>();

        public void QueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);
        public IEnumerable<HttpRequestMessage> GetRequests() => _requests;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
                throw new InvalidOperationException("No response configured");

            _requests.Add(request);
            var response = _responses.Dequeue();
            return Task.FromResult(response);
        }
    }
}
