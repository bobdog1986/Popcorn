using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using Popcorn.Services.Cache;
using Popcorn.Utils;

namespace Popcorn.Services.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            appBuilder.Use(async (context, next) =>
            {
                var response = context.Response;
                var request = context.Request;

                response.OnSendingHeaders(state =>
                {
                    var resp = (OwinResponse)state;
                    resp.Headers.Add("Access-Control-Allow-Origin", new[] {"*"});
                    resp.Headers.Add("Access-Control-Allow-Methods", new[] { "GET, POST, OPTIONS, PUT, DELETE" });
                }, response);

                await next();
            });

            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            appBuilder.UseWebApi(config);
            var cacheService = SimpleIoc.Default.GetInstance<ICacheService>();
            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                EnableDefaultFiles = false,
                FileSystem = new PhysicalFileSystem(cacheService.PopcornTemp),
                StaticFileOptions = { ContentTypeProvider = new CustomContentTypeProvider() }
            };

            appBuilder.UseFileServer(options);
        }
    }
}
