using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace sfintegration.infrastructure.Helper
{
    public sealed class BlobStorageHelper
    {
        static BlobStorageHelper _instance;
        static CloudBlobClient _blobClient;

        public static BlobStorageHelper Instance
        {
            get { return _instance ?? (_instance = new BlobStorageHelper()); }
        }

        private BlobStorageHelper()
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageAccount"));
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public CloudBlob GetBlob(string containerName, string key)
        {
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);

            return container.GetBlobReference(key);
        }

        public bool BlobExists(string containerName, string key)
        {
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);

            return container.GetBlockBlobReference(key).Exists();
        }

        public void UploadGZipStream(string containerName, string key, MemoryStream gZippedMemoryStream)
        {
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);

            container.CreateIfNotExists();

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(key);
            blockBlob.Properties.ContentEncoding = "gzip";
            blockBlob.Properties.ContentType = "text/json";
            blockBlob.UploadFromStream(gZippedMemoryStream);
            gZippedMemoryStream.Close();
        }

    }
}
