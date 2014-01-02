using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;

namespace BackgroundWorkers.Demo.Handlers
{
    class ScrapePage : Handler<ScrapePageMessage>
    {
        public override async Task Run(ScrapePageMessage message)
        {
            var url = message.Url;

            using (var connection = ConnectionProvider.Connect())
            {
                if (connection.Query<int>("select count(1) from Urls where Url = @Url", new { url }).Single() != 0)
                {
                    return;
                }

                connection.Execute("insert into Urls (Url) values (@url)", new { url });
            }

            var html = await HttpClientHelper.RequestAsString(url);
            if (html != null)
            {
                var regx = new Regex(@"https?://([-\w\.]+)+(:\d+)?(/([-\w/_\.]*(\?\S+)?)?)?", RegexOptions.IgnoreCase);

                foreach (Match match in regx.Matches(html))
                {
                    NewWorkItems.Add(new ScrapePageMessage {Url = match.Value});
                }

                var filename = ScreenshotService.Capture(url, "client");
                AppHub.NewPage(filename, url);
                //NewWorkItems.Add(new CapturePageMessage {Url = url});
            }
        }

        public override void OnComplete(ScrapePageMessage message)
        {
            Console.WriteLine("Discovered {0} links on {1}", NewWorkItems.Count, message.Url);
            using (var connection = ConnectionProvider.Connect())
            {
                connection.EnlistTransaction(Transaction.Current);
                connection.Execute("update Urls set Links = @Links where Url = @Url", new { message.Url, Links = NewWorkItems.Count });
            }
        }
    }

    public class ScrapePageMessage
    {
        public string Url { get; set; }
    }
}
