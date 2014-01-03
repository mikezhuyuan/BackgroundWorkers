using System.Collections.Generic;
using System.Transactions;
using BackgroundWorkers.Demo.Handlers;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace BackgroundWorkers.Demo
{
    public class AppHub : Hub
    {
        public void Send(string url)
        {
            using (var scope = new TransactionScope())
            using (var client = WorkersConfiguration.Current.CreateClient())
            {
                client.Enqueue(new ScrapePageMessage { Urls = new List<string>{url} });
                scope.Complete();
            }
        }
        
        static IHubConnectionContext GetClients()
        {
            return GlobalHost.ConnectionManager.GetHubContext<AppHub>().Clients;
        }

        public static void NewPage(string imageUrl, string url)
        {
            GetClients().All.newPage(imageUrl, url);
        }

        public static void Log(string message)
        {
            GetClients().All.log(message);
        }

        public static void WorkItems(Dictionary<string, int> dict)
        {
            GetClients().All.workItmes(dict);
        }
    }
}