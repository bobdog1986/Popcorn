using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Popcorn.Services.Hub
{
    public class PopcornHubService : IPopcornHubService
    {
        private readonly HubConnection _connection;

        public PopcornHubService()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"{Utils.Constants.PopcornApi.Replace("/api", "/popcorn")}")
                .Build();
            _connection.On<int>("OnUserConnected", (message) =>
            {

            });
        }

        public async Task Start()
        {
            //await _connection.StartAsync();
        }
    }
}
