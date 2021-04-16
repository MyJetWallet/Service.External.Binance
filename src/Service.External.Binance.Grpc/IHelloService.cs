using System.ServiceModel;
using System.Threading.Tasks;
using Service.External.Binance.Grpc.Models;

namespace Service.External.Binance.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}