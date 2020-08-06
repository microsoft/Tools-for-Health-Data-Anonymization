using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MicrosoftFhir.Anonymizer.Core.PartitionedExecution
{
    public class FhirEnumerableReader<T> : IFhirDataReader<T>
    {
        private IEnumerator<T> _enumerator;

        public FhirEnumerableReader(IEnumerable<T> data)
        {
            _enumerator = data.GetEnumerator();
        }

        public Task<T> NextAsync()
        {
            if (_enumerator.MoveNext())
            {
                return Task.FromResult(_enumerator.Current);
            }
            else
            {
                return Task.FromResult<T>(default(T));
            }
        }
    }
}
