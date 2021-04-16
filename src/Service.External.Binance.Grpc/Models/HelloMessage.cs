using System.Runtime.Serialization;
using Service.External.Binance.Domain.Models;

namespace Service.External.Binance.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}