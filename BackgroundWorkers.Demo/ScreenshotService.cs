using System;
using System.Diagnostics;
using System.IO;

namespace BackgroundWorkers.Demo
{
    static class ScreenshotService
    {
        public static string Capture(string url, string imageFolder)
        {
            var filename = Guid.NewGuid() + ".png";
            var startInfo = new ProcessStartInfo
            {
                FileName = "phantomjs.exe",
                Arguments = String.Format("\"rasterize.js\" \"{0}\" \"{1}\"", url, Path.Combine(imageFolder, filename)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            var p = new Process {StartInfo = startInfo};

            try
            {
                p.Start();
                p.WaitForExit(30000);
            }
            catch
            {
                p.Kill();
            }

            return filename;
        }
    }
}
