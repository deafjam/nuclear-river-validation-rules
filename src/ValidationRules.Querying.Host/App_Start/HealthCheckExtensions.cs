using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace NuClear.ValidationRules.Querying.Host
{
    internal static class HealthCheckExtensions
    {
        public static void UseHealthChecks(this HttpConfiguration configuration, string path)
        {
            configuration.Routes.MapHttpRoute("healthcheck", path, null, null, new HealthCheckHandler());
        }

        private sealed class HealthCheckHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage
                {
                    Content = new StringContent("Healthy")
                });
        }
    }
}