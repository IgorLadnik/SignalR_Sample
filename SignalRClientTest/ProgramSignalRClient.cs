using System;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using SignalRBaseHubClientLib;
using ModelLib;
using RemoteInterfaces;

namespace SignalRClientTest
{
    class ProgramSignalRClient
    {
        private const string URL = "https://localhost:15001/hub/the1st"; 
                                   //"http://localhost:15000/hub/the1st";

        private static HubClient _hubClient;
        private static AutoResetEvent _ev = new AutoResetEvent(false);

       private static void Main(string[] args)
        {
            MainAsync();

            while (true)
            {
                Console.WriteLine("Press any key \"q\" to quit or any other key to call server...");
                var ch = Console.ReadKey().KeyChar;
                if (ch == 'q' || ch == 'Q')
                {
                    _hubClient.Cancel();
                    _ev.WaitOne();
                    break;
                }

                //var now = $"{DateTime.Now}";
                var br0 = _hubClient.InvokeAsync("ProcessDto",
                    new []
                    {
                        new Dto { ClientId = ".NETCoreClient", Data = 10, Args = new Arg1[]
                             {
                                new() { Id = "0", Arg2Props = new() { new() { Id = "0.0" }, new() { Id = "0.1" }, } },
                                new() { Id = "1", Arg2Props = new() { new() { Id = "1.0" }, new() { Id = "1.1" }, } },
                             } 
                        },
                        new Dto { ClientId = ".NETCoreClient", Data = 11 },
                        new Dto { ClientId = ".NETCoreClient", Data = 12 },
                    }).Result;
            }
        }

        private static async void MainAsync()
        {
            // Create hub client and connect to server
            _hubClient = await new HubClient(URL)
                    .RegisterInterface<IRemoteCall1>()
                    .StartConnectionAsync(retryIntervalMs: 1000, numOfAttempts: 15);
            
            // Client provides handler for server's call of method ReceiveMessage
            _hubClient.Connection.On("ReceiveMessage", (string s0, string s1) => Console.WriteLine($"{s0} {s1}"));

            //public int Foo(string name, Arg1[] arg1s)
            var task = _hubClient.RpcAsync("IRemoteCall1", "Foo", "theName", new Arg1[]
                {
                    new Arg1 { Id = "0", Arg2Props = new() { new() { Id = "0.0" }, new() { Id = "0.1" } } },
                    new Arg1 { Id = "1", Arg2Props = new() { new() { Id = "1.0" }, new() { Id = "1.1" } } }
                });

            // Client calls server's method ProcessDto
            var br0 = await _hubClient.InvokeAsync("ProcessDto",
            new[]
            {
                new Dto { ClientId = ".NETCoreClient", Data = 91, Args = new Arg1[]
                            {
                            new() { Id = "0", Arg2Props = new() { new() { Id = "0.0" }, new() { Id = "0.1" }, } },
                            new() { Id = "1", Arg2Props = new() { new() { Id = "1.0" }, new() { Id = "1.1" }, } },
                            }
                },
                new Dto { ClientId = ".NETCoreClient", Data = 92, Args = new Arg1[]
                            {
                            new() { Id = "0", Arg2Props = new() { new() { Id = "0.0" }, new() { Id = "0.1" }, } },
                            new() { Id = "1", Arg2Props = new() { new() { Id = "1.0" }, new() { Id = "1.1" }, } },
                            }
                },
            });

            var result = (int) await task;

            // Client subscribes for stream of Dto objects providing appropriate handler
            if (!await _hubClient.SubscribeAsync<Dto>(arg => Console.WriteLine(arg)))
                _ev.Set();     
        }
    }
}
