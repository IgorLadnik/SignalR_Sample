
using System;
using System.Collections.Generic;
using RemoteInterfaces;

namespace ModelLib
{
    public class Dto
    {
        public string ClientId { get; set; }

        public string Id { get; set; }

        public DtoKind Kind { get; set; }

        public DtoStatus Status { get; set; }

        public object Payload { get; set; }

        public int Data { get; set; }

        public override string ToString() =>
            $"{ClientId}     {Data}";

        public Arg1[] Args { get; set; }
    }

    public class MethodPayload
    {
        public string Name { get; set; }
        public object[] Args { get; set; }
    }

    public enum DtoKind
    {
        None = 0,
        Request = 1,
        Response = 2,
    }

    public enum DtoStatus
    {
        None = 0,
        Error = 1,
        Created = 2,
        Processed = 3,
    }
}
