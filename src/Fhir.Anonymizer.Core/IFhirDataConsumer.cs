using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core
{
    public interface IFhirDataConsumer
    {
        Task ConsumeAsync(IEnumerable<string> data);
    }
}
