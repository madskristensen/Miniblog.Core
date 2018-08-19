using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace Miniblog.Core.Services
{
    public class AzureBlobFileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly string _container;

        public AzureBlobFileService(IConfiguration configuration)
        {
            _configuration = configuration;
            _container = _configuration["azureBlobStore:container"];
        }
        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration["azureBlobStore:connectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_container);

            if (!await blobContainer.ExistsAsync())
            {
                await blobContainer.CreateAsync();
                await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }

            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(fileName);
            if (await blob.ExistsAsync())
            {
                var fName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                var ext = System.IO.Path.GetExtension(fileName);
                var it = 0;
                do
                {
                    it++;
                    blob = blobContainer.GetBlockBlobReference(fName + "_" + it + ext);
                } while (await blob.ExistsAsync());
                fileName = fName + "_" + it + ext;
            }

            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);

            blob.Properties.CacheControl = "max-age=604800";
            await blob.SetPropertiesAsync();

            return $"{_configuration["azureBlobStore:publicUrl"]}/{_container}/{fileName}";
        }
    }
}
