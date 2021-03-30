using System;
//using Microsoft.Extensions.Logging;
using RemoteInterfaces;

namespace RemoteImplementations
{
    public class RemoteCall1 : IRemoteCall1//, ILog
    {
        //private ILogger _logger;
        //public ILoggerFactory LoggerFactory { set => _logger = value?.CreateLogger<RemoteCall2>(); }

        public RetOuter Foo(string name, Arg1[] arg1s)
        {
            //_logger?.LogInformation("*** RemoteCall1.Foo()");
            return 
                
                    new RetOuter 
                    {
                        inners = new RetInner[] 
                        { 
                            new() { id = "0_00" }, new() { id = "0_01" },
                            new() { id = "0_10" }, new() { id = "0_11" }
                        }
                    }/*,
                    new RetOuter
                    {
                        inners = new RetInner[]
                        {
                            new() { id = "1_00" }, new() { id = "1_01" },
                            new() { id = "1_10" }, new() { id = "1_11" }
                        }
                    }*/
                ;
        }

        public string Echo(string text) => $"Echo1: {text}";
    }

    //public class RemoteCall2 : IRemoteCall2, IDirectCall, ILog
    //{
    //    private static int objectsCount = 0;

    //    private int _id;
    //    private ILogger _logger;

    //    public ILoggerFactory LoggerFactory { set => _logger = value?.CreateLogger<RemoteCall2>(); }

    //    public RemoteCall2()
    //    {
    //        _id = ++objectsCount;
    //        //Console.WriteLine($"    *** RemoteCall2.Ctor() -> {_id}");
    //    }

    //    #region IRemoteCall2 implementation

    //    public int Foo(string name, Arg1[] arg1s)
    //    {
    //        _logger?.LogInformation($"    *** RemoteCall2.Foo() -> {_id}");
    //        return _id;
    //    }

    //    public string Echo(string text) 
    //    {
    //        var echo = $"Echo2: {text}";
    //        _logger?.LogInformation($"    *** Echo -> {echo}");
    //        return echo;
    //    }

    //    #endregion // IRemoteCall2 implementation

    //    public object DirectCall(string methodName, params object[] args)
    //    {
    //        switch (methodName)
    //        {
    //            case "Foo":
    //                return Foo((string)args[0], (Arg1[])args[1]);

    //            case "Echo":
    //                return Echo((string)args[0]);
    //        }

    //        return null;
    //    }
    //}
}
