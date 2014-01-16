using System.Web.Http;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json.Serialization;
using Owin;

namespace BackgroundWorkers.Demo
{
    public class WebAppStartup
    {
        public void Configuration(IAppBuilder builder)
        {
            var fs = new PhysicalFileSystem("client");

            builder.UseDefaultFiles(new DefaultFilesOptions { FileSystem = fs });
            builder.UseStaticFiles(new StaticFileOptions { FileSystem = fs });

            var config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                "api",
                "api/{controller}/{id}",
                new { id = RouteParameter.Optional }
                );

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            builder.MapSignalR();
        }
    }
}