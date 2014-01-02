using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BackgroundWorkers.Demo
{
    static class HttpClientHelper
    {
        public static async Task<string> RequestAsString(string url)
        {
            var baseAddress = new Uri(url);
            using (var handler = new HttpClientHandler() { AllowAutoRedirect = true })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            using (var request = new HttpRequestMessage(HttpMethod.Get, baseAddress))
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType == "text/html")
                    return await response.Content.ReadAsStringAsync();
            }

            return await Task.FromResult((string)null);
        }
    }
}
