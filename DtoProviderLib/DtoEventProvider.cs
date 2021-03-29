using System;
using System.Threading;
using ModelLib;
using SignalRBaseHubServerLib;

namespace DtoProviderLib
{
    public class DtoEventProvider : StreamingDataProvider<Dto>
    {
        private const int rndLowLimit = 0;
        private const int rndUpperLimit = 999;
        private const int intervalInMs = 3000;

        private Timer _timer;
        private Random _random = new(11);

        private static DtoEventProvider _helper;
        public static DtoEventProvider Instance => _helper ??= new();

        private DtoEventProvider() =>
            _timer = new Timer(_ => Current = new() { ClientId = $"{Guid.NewGuid()}", Data = _random.Next(rndLowLimit, rndUpperLimit) }, null, 0, intervalInMs);
    }
}
