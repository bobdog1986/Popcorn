using System.Threading.Tasks;
using Popcorn.Chromecast.Interfaces;

namespace Popcorn.Chromecast.Controllers
{
    public abstract class BaseController : IController
    {
        public string ApplicationId { get; set; }
        public async Task LaunchApplication()
        {
            await Client.ReceiverChannel.LaunchApplication(ApplicationId);
        }

        protected readonly ChromeCastClient Client;

        protected BaseController(ChromeCastClient client, string applicationId)
        {
            Client = client;
            ApplicationId = applicationId;
        }

        public async Task StopApplication()
        {
            await Client.ReceiverChannel.StopApplication();
        }

    }
}
