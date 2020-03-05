using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fhir.Anonymizer.Core.PartitionedExecution
{
    public class FhirStreamConsumer : IFhirDataConsumer, IDisposable
    {
        private StreamWriter _writer;

        public FhirStreamConsumer(Stream stream)
        {
            _writer = new StreamWriter(stream);
        }

        public async Task CompleteAsync()
        {
            _writer.Flush();
        }

        public async Task ConsumeAsync(IEnumerable<string> data)
        {
            foreach (string content in data)
            {
                await _writer.WriteLineAsync(content);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
