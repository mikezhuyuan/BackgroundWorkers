using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Dapper;

namespace BackgroundWorkers.Demo.Handlers
{
    class ScrapePage : Handler<ScrapePageMessage>
    {
        public override async Task Run(ScrapePageMessage message)
        {
            var url = message.PopRandom();

            var html = await HttpClientHelper.RequestAsString(url);
            if (html != null)
            {
                var regx = new Regex(@"https?://([-\w\.]+)+(:\d+)?(/([-\w/_\.]*(\?[^\s""']+)?)?)?", RegexOptions.IgnoreCase);

                NewWorkItems.Add(new CapturePageMessage {Url = url});

                var urls = new List<string>();
                foreach (Match match in regx.Matches(html))
                {
                    var url2 = HttpUtility.HtmlDecode(match.Value);
                    if(BlackList.ShouldVisit(url2))
                        urls.Add(url2);
                }

                if (urls.Any())
                    NewWorkItems.Add(new ScrapePageMessage {Urls = urls});
            }
            else
            {
                BlackList.Block(url);
                if(message.Urls.Any())
                    NewWorkItems.Add(message);
            }

            if (message.Urls.Any())
            {
                int ready;
                using (var conn = ConnectionProvider.Connect())
                {
                    ready = conn.Query<int>(@"select count(1) from workitems where status = 0").Single();
                }

                while (ready < 10)
                {
                    NewWorkItems.Add(message);
                    ready++;
                }
            }
        }
    }

    public class ScrapePageMessage
    {
        static readonly Random Rnd = new Random();

        public List<string> Urls { get; set; }

        public string PopRandom()
        {
            var index = Rnd.Next(Urls.Count);
            var url = Urls[index];
            Urls.RemoveAt(index);

            return url;
        }
    }
}
