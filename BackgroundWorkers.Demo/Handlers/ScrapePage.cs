using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;

namespace BackgroundWorkers.Demo.Handlers
{
    class ScrapePage : Handler<ScrapePageMessage>
    {
        public override async Task Run(ScrapePageMessage message)
        {
            using (var connection = ConnectionProvider.Connect())
            {
                if (connection.Query<int>("select count(1) from Urls where Url = @Url", new { message.Url }).Single() != 0)
                {
                    return;
                }

                connection.Execute("insert into Urls (Url) values (@url)", new { message.Url });
            }

            var html = await HttpClientHelper.RequestAsString(message.Url);
            if (html != null)
            {
                var regx = new Regex("href=\"([^\"]*)", RegexOptions.IgnoreCase);

                var @this = new Uri(message.Url);

                foreach (Match match in regx.Matches(html))
                {
                    var href = match.Groups[1].Value;
                    var target = new Uri(href, UriKind.RelativeOrAbsolute);

                    if (target.IsAbsoluteUri)
                    {
                        if (StringComparer.InvariantCultureIgnoreCase.Compare(@this.Host, target.Host) == 0)
                        {
                            NewWorkItems.Add(new ScrapePageMessage { Url = href });
                            NewWorkItems.Add(new CapturePageMessage { Url = href });
                        }
                    }
                    else
                    {
                        var builder = new UriBuilder
                        {
                            Scheme = @this.Scheme,
                            Host = @this.Host,
                            Port = @this.Port,
                            Path = href
                        };

                        NewWorkItems.Add(new ScrapePageMessage { Url = builder.Uri.ToString() });
                        NewWorkItems.Add(new CapturePageMessage { Url = builder.Uri.ToString() });

                    }
                }
            }
        }
    }

    public class ScrapePageMessage
    {
        public string Url { get; set; }
    }
}
