using System;
using System.Threading.Tasks;

namespace Popcorn.Services.FileServer
{
    public interface IFileServerService
    {
        Task<Func<object, Task<object>>> StartStreamFileServer(string filePath, string contentType, int port);
        Task<Func<object, Task<object>>> StartStaticFileServer(string filePath, string contentType, int port);
    }
}
