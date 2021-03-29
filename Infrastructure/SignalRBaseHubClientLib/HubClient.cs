using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRBaseHubClientLib
{
    public class HubClient : IDisposable
    {
        public string Url { get; }

        public HubConnection Connection { get; protected set; }
        protected CancellationTokenSource _cts = new();

        public HubClient(string url) => 
            Url = url; 

        public async Task<HubClient> StartConnectionAsync(int retryIntervalMs = 0, int numOfAttempts = 0)
        {
            Connection = new HubConnectionBuilder().WithUrl(Url).Build();

            for (var i = 0; i < numOfAttempts; i++)
            {
                try
                {
                    await Connection.StartAsync();
                    return this;
                }
                catch (Exception e)
                {
                    if (i < numOfAttempts - 1)
                        await Task.Delay(numOfAttempts);
                    else
                        throw new Exception($"Hub connection on \"{Url}\" had failed. ", e);
                }
            }

            return null;
        }
        
        public async Task<bool> InvokeAsync(string methodName, params object[] args)
        {
            if (Connection == null || _cts.Token.IsCancellationRequested || args == null)
                return false;

            try
            {
                await Connection.InvokeAsync(methodName, args, _cts.Token);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"Hub\"{Url}\" InvokeAsync() of method \"{methodName}()\" had failed. ", e);
            }
        }

        public async Task<bool> SubscribeAsync<T>(Action<T> callback)
        {
            if (Connection == null || _cts.Token.IsCancellationRequested || callback == null)
                return false;

            try
            {
                var channel = await Connection.StreamAsChannelAsync<T>("StartStreaming", _cts.Token);
                while (await channel.WaitToReadAsync())
                    while (channel.TryRead(out var t))
                    {
                        try
                        {
                            callback(t);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Hub \"{Url}\" Subscribe(): callback had failed. ", e);
                        }
                    }

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public void Cancel() => 
            _cts.Cancel();

        public void Dispose() => 
            Connection.DisposeAsync().Wait();
    }
}
