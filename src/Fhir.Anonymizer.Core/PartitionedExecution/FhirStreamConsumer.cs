using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            await _writer.FlushAsync().ConfigureAwait(false);
        }

        public async Task ConsumeAsync(IEnumerable<string> data)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string content in data)
            {
                builder.AppendLine(content);
            }

            await _writer.WriteAsync(builder.ToString()).ConfigureAwait(false);
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _writer?.Dispose();
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
