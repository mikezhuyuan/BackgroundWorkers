using System.Threading.Tasks;

namespace BackgroundWorkers.Demo.Handlers
{
    class CapturePage : Handler<CapturePageMessage>
    {
        public override Task Run(CapturePageMessage message)
        {
            var url = message.Url;
            var filename = ScreenshotService.Capture(url, "client");
            AppHub.NewPage(filename, url);

            return Task.FromResult((object)null);
        }
    }

    public class CapturePageMessage
    {
        public string Url { get; set; }
    }
}
