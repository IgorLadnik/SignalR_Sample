using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ModelLib;

namespace SignalRBaseHubClientLib
{
    public class HubClient : IDisposable
    {
        public string Url { get; }
        public string ClientId { get; }

        public HubConnection Connection { get; protected set; }
        protected CancellationTokenSource _cts = new();

        public HubClient(string url, string cleintId = null)
        {
            Url = url;
            ClientId = string.IsNullOrWhiteSpace(cleintId) ? $"{Guid.NewGuid()}" : cleintId;
        }

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

        public async Task<object> RpcAsync(string interfaceName, string methodName, params object[] args)
        {
            if (Connection == null || _cts.Token.IsCancellationRequested || args == null)
                return null;

            RpcDto rpcArgs = new()
            {
                ClientId = ClientId,
                Id = $"{Guid.NewGuid()}",
                Kind = DtoKind.Request,
                Status = DtoStatus.Created,
                InterfaceName = interfaceName,
                MethodName = methodName,
                Args = args?.Select(a => new DtoData { TypeName = a.GetType().FullName, Data = a })?.ToArray()
            };

            try
            {
                var result = await Connection.InvokeAsync<object>("ProcessRpc", rpcArgs, _cts.Token);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception($"Hub\"{Url}\" InvokeAsync() of method \"{methodName}()\" had failed. ", e);
            }
        }

        public async Task<object> InvokeAsync(string methodName, params object[] args)
        {
            if (Connection == null || _cts.Token.IsCancellationRequested || args == null)
                return null;

            try
            {
                var result = await Connection.InvokeAsync<object>(methodName, args, _cts.Token);
                return result;
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
