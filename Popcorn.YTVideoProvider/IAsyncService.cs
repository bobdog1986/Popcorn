using System.Collections.Generic;
using System.Threading.Tasks;

namespace Popcorn.YTVideoProvider
{
    internal interface IAsyncService<T> where T : Video
    {
        Task<T> GetVideoAsync(string uri);
        Task<IEnumerable<T>> GetAllVideosAsync(string uri);
    }
}