namespace MicrosoftFhir.Anonymizer.AzureDataFactoryPipeline.src
{
    public static class FhirAzureConstants
    {
        public const int KB = 1024;
        public const int MB = KB * 1024;
        public const int BlockBufferSize = 4 * MB;
        public const int DefaultConcurrentCount = 3;
        public const int DefaultBlockDownloadTimeoutInSeconds = 5 * 60;
        public const int DefaultBlockDownloadTimeoutRetryCount = 3;
        public const int DefaultUploadBlockThreshold = 32 * MB;
        public const int DefaultBlockUploadTimeoutInSeconds = 10 * 60;
        public const int DefaultBlockUploadTimeoutRetryCount = 3;
        public const int StorageOperationRetryDelayInSeconds = 30;
        public const int StorageOperationRetryMaxDelayInSeconds = 120;
        public const int StorageOperationRetryCount = 3;
    }
}
