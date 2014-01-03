using System;
using System.Diagnostics;
using System.IO;
using ImageResizer;

namespace BackgroundWorkers.Demo
{
    static class ScreenshotService
    {
        public static string Capture(string url, string imageFolder)
        {
            var filename = Guid.NewGuid() + ".png";
            var path = Path.Combine(imageFolder, filename);
            var startInfo = new ProcessStartInfo
            {
                FileName = "phantomjs.exe",
                Arguments = String.Format("\"rasterize.js\" \"{0}\" \"{1}\"", url, path),
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

                if (File.Exists(path))
                {
                    //make thumbnail
                    filename = Guid.NewGuid() + ".png";
                    var src = path;
                    var dest = Path.Combine(imageFolder, filename);

                    ImageBuilder.Current.Build(src, dest,
                        new ResizeSettings(128, 128, FitMode.Stretch, "png"));

                    File.Delete(src);
                }
                else
                {
                    return "error.jpg";
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                p.Kill();
                return "error.jpg";
            }

            return filename;
        }
    }
}
