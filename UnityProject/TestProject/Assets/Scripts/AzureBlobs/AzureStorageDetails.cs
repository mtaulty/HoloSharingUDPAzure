// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
namespace SharedHolograms.AzureBlobs
{
    using System;
    using System.IO;

    [Serializable]
    public class AzureStorageDetails
    {
        // Public fields to keep Unity happy...
        public string AzureAccountName;
        public string AzureKeyBase64;
        public string AzureContainerName;

        public byte[] AzureKey
        {
            get
            {
                return (Convert.FromBase64String(this.AzureKeyBase64));
            }
        }

        // https://docs.microsoft.com/en-us/rest/api/storageservices/fileservices/versioning-for-the-azure-storage-services
        public string Version
        {
            get
            {
                return ("2016-05-31");
            }
        }
        public AzureStorageDetails()
        {
        }
        public string PrimaryEndpoint
        {
            get
            {
                return "https://" + this.AzureAccountName + ".blob.core.windows.net/";
            }
        }
        public string SecondaryEndpoint
        {
            get
            {
                return "https://" + this.AzureAccountName + "-secondary.blob.core.windows.net/";
            }
        }
        public string GetFullPathForResource(string resource)
        {
            return (this.AzureContainerName + "/" + resource);
        }
    }
}