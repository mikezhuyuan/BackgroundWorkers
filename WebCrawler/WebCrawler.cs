using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using BackgroundWorkers;
using Dapper;

namespace WebCrawler
{
    public class WebCrawler : Handler<UrlMessage>
    {
        
        public override async Task Run(UrlMessage message)
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["WebCrawler"].ConnectionString))
            {
                connection.Open();
                if (connection.Query<int>("select count(1) from Urls where Url = @Url", new {message.Url}).Single() != 0)
                {
                    return;
                }

                connection.Execute("insert into Urls (Url) values (@url)", new {message.Url});
            }

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
            using (var handler = new HttpClientHandler() { AllowAutoRedirect = true })
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
            Console.WriteLine("Discovered {0} links on {1}", NewWorkItems.Count, message.Url);
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["WebCrawler"].ConnectionString))
            {
                connection.Open();
                connection.EnlistTransaction(Transaction.Current);
                connection.Execute("update Urls set Links = @Links where Url = @Url", new { message.Url, Links = NewWorkItems.Count });
            }
        }
    }

    public class UrlMessage
    {
        public string Url { get; set; }
    }

}