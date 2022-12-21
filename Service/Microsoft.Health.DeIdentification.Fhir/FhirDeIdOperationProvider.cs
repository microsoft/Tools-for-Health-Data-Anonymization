using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using System.Text;
using System.Threading.Channels;
using Xunit.Sdk;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperationProvider : IDeIdOperationProvider
    {
        private readonly int outputChannedlLimit = 100;
        private readonly string pathPrefix = "../Microsoft.Health.DeIdentification.Local/configurations/";
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdConfiguration deIdConfiguration)
        {
            throw new NotImplementedException();
        }

        public List<FhirDeIdOperation> CreateDeIdOperations(DeIdConfiguration deIdConfiguration)
        {
            List<FhirDeIdOperation> operations = new List<FhirDeIdOperation>();
            foreach (var item in deIdConfiguration.ModelConfigReferences)
            {
                operations.Add(new FhirDeIdOperation($"{pathPrefix+item.Value}"));
            }
            return operations;
        }
    }
}