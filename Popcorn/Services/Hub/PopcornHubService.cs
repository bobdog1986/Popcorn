using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;

namespace Popcorn.Services.Hub
{
    public class PopcornHubService : IPopcornHubService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private readonly HubConnection _connection;

        public PopcornHubService()
        {
            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl($"{Utils.Constants.PopcornApi.Replace("/api", "/popcorn")}")
                    .Build();
                _connection.On<int>("OnUserConnected", message =>
                {

                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task Start()
        {
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
