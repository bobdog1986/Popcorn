using System.Threading.Tasks;

namespace Popcorn.Chromecast.Interfaces
{
    public interface  IController
    {
        string ApplicationId { get; set; }
        Task LaunchApplication();
    }
}
