using System.ServiceModel;

namespace Contracts
{
    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        string GetData(int value);
    }
}
