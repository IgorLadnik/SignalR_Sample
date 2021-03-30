using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelLib
{
    public class RpcDto
    {
        public string ClientId { get; set; }

        public string Id { get; set; }

        public DtoKind Kind { get; set; }

        public DtoStatus Status { get; set; }

        public string InterfaceName { get; set; }

        public string MethodName { get; set; }

        public DtoData[] Args { get; set; }
    }

    public class DtoData 
    {
        public string TypeName { get; set; }
        public object Data { get; set; }
    }
}
