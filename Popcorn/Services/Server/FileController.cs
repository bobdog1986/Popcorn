using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Popcorn.Services.Server
{
    public class FileController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get(string path)
        {
            var video = new VideoStream(Uri.UnescapeDataString(path));
            Func<Stream, HttpContent, TransportContext, Task> func = video.WriteToStream;
            var response = Request.CreateResponse();
            response.Content = new PushStreamContent(func, new MediaTypeHeaderValue("video/mp4"));
            return response;
        }
    }
}
