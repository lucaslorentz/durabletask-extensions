using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.Server.Tests
{
    class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;

            return response;
        }
    }
}
