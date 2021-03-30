using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using AsyncAutoResetEventLib;
using ModelLib;
using System.Threading.Tasks;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace SignalRBaseHubServerLib
{
    public class StreamingHub<T> : Hub, ISetEvent
    {
        class Descriptor
        {
            public Type type;
            public object ob;
            public bool isPerSession = false;
            public Dictionary<string, PerSessionDescriptor> dctSession;
            public Dictionary<string, Type> dctType;
        }

        class PerSessionDescriptor
        {
            public object ob;

            private long _lastActivationInTicks;
            public long lastActivationInTicks
            {
                get => Interlocked.Read(ref _lastActivationInTicks);
                set => Interlocked.Exchange(ref _lastActivationInTicks, value);
            }
        }

        protected readonly IStreamingDataProvider<T> _streamingDataProvider;
        private readonly AsyncAutoResetEvent _aev = new();

        private int _isValid = 0;

        private static Dictionary<string, Descriptor> _dctInterface = new();

        private static Timer _timer;
        //private readonly ILogger _logger;
        //private readonly ILoggerFactory _loggerFactory;

        protected StreamingHub(StreamingDataProvider<T> streamingDataProvider)
        {
            IsValid = true;
            streamingDataProvider.Add(this);
            _streamingDataProvider = streamingDataProvider;
        }

        public static void RegisterSingleton<TInterface, TImpl>() where TImpl : TInterface, new() =>
            Register(typeof(TInterface), typeof(TImpl));

        public static void RegisterPerCall<TInterface, TImpl>() where TImpl : TInterface, new() =>
            Register(typeof(TInterface), typeof(TImpl));

        public static void RegisterPerSession<TInterface, TImpl>(int sessionLifeTimeInMin = -1) where TImpl : TInterface, new() =>
            Register(typeof(TInterface), typeof(TImpl), true, sessionLifeTimeInMin);


        private static void Register(Type @interface, Type impl, bool isPerSession = false, int sessionLifeTimeInMin = -1)
        {
            //_logger.LogInformation($"About to register interface '{@interface.Name}' with type '{impl.Name}', isPerSession = {isPerSession}, sessionLifeTimeInMin = {sessionLifeTimeInMin}");
            _dctInterface[@interface.Name] = new()
            {
                type = impl,
                isPerSession = isPerSession,
                dctType = GetTypeDictionary(@interface), 
            };

            if (isPerSession && sessionLifeTimeInMin > 0 && _timer == null)
            {
                var sessionLifeTime = TimeSpan.FromMinutes(sessionLifeTimeInMin);
                _timer = new(_ =>
                {
                    var now = DateTime.UtcNow;
                    foreach (var dict in _dctInterface.Values?.Where(d => d.isPerSession)?.Select(d => d.dctSession))
                    {
                        if (dict == null || dict.Count == 0)
                            continue;

                        foreach (var clientId in dict.Keys.ToArray())
                            if (now - new DateTime(dict[clientId].lastActivationInTicks) > sessionLifeTime)
                                dict.Remove(clientId, out PerSessionDescriptor psd);
                    }
                },
                null, TimeSpan.Zero, TimeSpan.FromMinutes(sessionLifeTimeInMin));
            }

            //_logger.LogInformation($"Registered interface '{@interface.Name}' with type '{impl.Name}', isPerSession = {isPerSession}, sessionLifeTimeInMin = {sessionLifeTimeInMin}");
        }

        private static Dictionary<string, Type> GetTypeDictionary(Type interfaceType)
        {
            Dictionary<string, Type> dctType = new();
            foreach (var mi in interfaceType.GetMethods())
                foreach (var pi in mi.GetParameters())
                    if (Filter(pi.ParameterType))
                        dctType[pi.ParameterType.FullName] = pi.ParameterType;
            return dctType;
        }

        private static bool Filter(Type type)
        {
            return !type.FullName.Contains("System.");
        }

        private static object[] GetMethodArguments(RpcDto arg)
        {
            if (!_dctInterface.TryGetValue(arg.InterfaceName, out Descriptor descriptor))
                return null;

            List<object> methodParams = new();
            foreach (var dtoData in arg?.Args)
            {
                var je = (JsonElement)dtoData.Data;
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        methodParams.Add(je.GetString());
                        continue;
                }

                if (!descriptor.dctType.TryGetValue(dtoData.TypeName, out Type type))
                    throw new Exception($"Type '{dtoData.TypeName}' is not registered");

                methodParams.Add(JsonSerializer.Deserialize(je.GetRawText(), type, new() { PropertyNameCaseInsensitive = true }));
            }

            return methodParams.ToArray();
        }

        private object Resolve(string interafceName, string clientId = null)
        {
            if (!_dctInterface.TryGetValue(interafceName, out Descriptor descriptor))
                return null;

            if (descriptor.ob != null)
                // Singleton
                return descriptor.ob;

            if (descriptor.type != null)
            {
                if (!descriptor.isPerSession || string.IsNullOrEmpty(clientId))
                    // Per Call
                    return CreateInstanceWithLoggerIfSupported(descriptor.type);

                // Per Session
                if (descriptor.dctSession == null)
                    descriptor.dctSession = new();

                if (descriptor.dctSession.TryGetValue(clientId, out PerSessionDescriptor perSessionDescriptor))
                {
                    perSessionDescriptor.lastActivationInTicks = DateTime.UtcNow.Ticks;
                    return perSessionDescriptor.ob;
                }

                descriptor.dctSession[clientId] = perSessionDescriptor = new()
                {
                    ob = CreateInstanceWithLoggerIfSupported(descriptor.type),
                    lastActivationInTicks = DateTime.UtcNow.Ticks,
                };

                return perSessionDescriptor.ob;
            }

            return null;
        }

        private object CreateInstanceWithLoggerIfSupported(Type type) =>
            //AssignLoggerIfSupported(Activator.CreateInstance(type));
            Activator.CreateInstance(type);

        //private object AssignLoggerIfSupported(object ob)
        //{
        //    var log = ob as ILog;
        //    if (log != null)
        //        log.LoggerFactory = _loggerFactory;
        //    return ob;
        //}

        public virtual object ProcessRpc(RpcDto arg)
        {
            if (!_dctInterface.TryGetValue(arg.InterfaceName, out Descriptor descriptor))
                throw new Exception($"Interface '{arg.InterfaceName}' is not regidtered");

            var methodArgs = GetMethodArguments(arg);
            var localOb = Resolve(arg.InterfaceName, arg.ClientId);

            var methodInfo = localOb?.GetType().GetMethod(arg.MethodName);
            var retOb = methodInfo?.Invoke(localOb, methodArgs);
            //await Clients.All.SendAsync("ReceiveMessage", "...", retOb.ToString());
            return retOb;
        }

        //return await Task.Run(() => new object());

        //StringBuilder sbClients = new();
        //StringBuilder sbData = new();

        //if (args != null && args.Length > 0)
        //{
        //    sbClients.Append($"{Environment.NewLine}Clients: ");
        //    foreach (var clientId in args.Select(dto => dto.ClientId).Distinct())
        //        sbClients.Append($"{clientId} ");

        //    sbData.Append("--> Data: ");
        //    foreach (var dto in args)
        //        sbData.Append($"{dto.Data} ");
        //}
        //else
        //{
        //    sbClients.Append("No clients");
        //    sbData.Append("No data available");
        //}

        //await Clients.All.SendAsync("ReceiveMessage", sbClients.ToString(), sbData.ToString());

        //return args;
        //}

        public ChannelReader<T> StartStreaming() =>
            Observable.Create<T>(async observer =>
            {
                while (!Context.ConnectionAborted.IsCancellationRequested)
                {               
                    await _aev.WaitAsync();
                    observer.OnNext(_streamingDataProvider.Current);
                }
            }).AsChannelReader();

        public bool IsValid
        {
            get => Interlocked.Exchange(ref _isValid, _isValid) == 1;
            private set => Interlocked.Exchange(ref _isValid, value ? 1 : 0);
        }
        
        public void SetEvent() =>
            _aev.Set();

        protected override void Dispose(bool disposing)
        {
            IsValid = false;
            base.Dispose(disposing);
        }
    }
}
