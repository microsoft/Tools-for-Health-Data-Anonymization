using Microsoft.Health.DeIdentification.Batch;
using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Azure
{
    // This loader can be used for DICOM
    public class AzureBlobBinaryLoader : DataLoader<string>
    {
        protected override Task LoadDataInternalAsync(Channel<string> outputChannel, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
