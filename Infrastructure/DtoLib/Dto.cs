using RemoteInterfaces;

namespace DtoLib
{
    public class Dto
    {
        public string ClientId { get; set; }

        public string Id { get; set; }

        public DtoStatus Status { get; set; }

        public object Payload { get; set; }

        public int Data { get; set; }

        public override string ToString() =>
            $"{ClientId}     {Data}";

        public Arg1[] Args { get; set; }
    }
}
