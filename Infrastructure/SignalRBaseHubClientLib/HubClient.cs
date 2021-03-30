using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using DtoLib;

namespace SignalRBaseHubClientLib
{
    public class HubClient : IDisposable
    {
        public string Url { get; }
        public string ClientId { get; }

        public HubConnection Connection { get; protected set; }
        protected CancellationTokenSource _cts = new();

        private Dictionary<string, Type> _dctType = new();

        public HubClient(string url, string cleintId = null)
        {
            Url = url;
            ClientId = string.IsNullOrWhiteSpace(cleintId) ? $"{Guid.NewGuid()}" : cleintId;
        }

        #region Type manipulations

        public HubClient RegisterInterface<TInterface>() where TInterface : class
        {
            foreach (var mi in typeof(TInterface).GetMethods())
            {
                var rt = mi.ReturnType;
                _dctType[rt.FullName] = rt;
            }

            return this;
        }

        private object GetResult(JObject jo)
        {
            if (jo == null)
                return null;

            if (!jo.TryGetValue("result", out JToken jt))
                return null;

            var typFullName = jt.First().Values<string>().First();
            var result = jt.Last().Values<object>().First();

            return JsonSerializer.Deserialize($"{(JObject)result}", _dctType[typFullName], new() { PropertyNameCaseInsensitive = true });
        }

        #endregion // Type manipulations

        #region StartConnection, Subscribe

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

        #endregion // StartConnection, Subscribe

        #region Invoke, Rpc

        public async Task<object> RpcAsync(string interfaceName, string methodName, params object[] args)
        {
            if (Connection == null || _cts.Token.IsCancellationRequested || args == null)
                return null;

            RpcDtoRequest rpcArgs = new()
            {
                ClientId = ClientId,
                Id = $"{Guid.NewGuid()}",
                Status = DtoStatus.Created,
                InterfaceName = interfaceName,
                MethodName = methodName,
                Args = args?.Select(a => new DtoData { TypeName = a.GetType().FullName, Data = a })?.ToArray()
            };

            try
            {
                var result = await Connection.InvokeAsync<object>("ProcessRpc", rpcArgs, _cts.Token);
                return GetResult((JObject)result);
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

        #endregion // Invoke, Rpc

        #region Cancel, Dispose

        public void Cancel() => 
            _cts.Cancel();

        public void Dispose() => 
            Connection.DisposeAsync().Wait();

        #endregion // Cancel, Dispose
    }
}
