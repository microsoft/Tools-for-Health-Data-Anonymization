using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.FreeText
{
    public class FreeTextDeIdOperation : IDeIdOperation<string, string>
    {
        public string Process(string source)
        {
            throw new NotImplementedException();
        }
    }
}