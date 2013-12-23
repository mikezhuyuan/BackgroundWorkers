using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BackgroundWorkers;

namespace WebCrawler
{
    public class WebCrawler : Handler<UrlMessage>
    {
        
        public override async Task Run(UrlMessage message)
        {
            var html = await RequestAsString(message.Url);
            var regx = new Regex(@"https?://([-\w\.]+)+(:\d+)?(/([-\w/_\.]*(\?\S+)?)?)?", RegexOptions.IgnoreCase);

            foreach (Match match in regx.Matches(html))
            {
                NewWorkItems.Add(new UrlMessage { Url = match.Value });
            }

        }

        async Task<string> RequestAsString(string url)
        {
            Console.WriteLine("Crawling: {0}", url);
            var baseAddress = new Uri(url);
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            using (var request = new HttpRequestMessage(HttpMethod.Get, baseAddress))
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType == "text/html")
                    return await response.Content.ReadAsStringAsync();
            }

            return await Task.FromResult(string.Empty);
        }

        public override void OnComplete(UrlMessage message)
        {
            Console.WriteLine("Discovered {0} links", NewWorkItems.Count);
        }
    }

    public class UrlMessage
    {
        public string Url { get; set; }
    }

}