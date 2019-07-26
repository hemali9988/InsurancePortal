using InsuranceClientPortal.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClientPortal.Helpers
{
    public class StorageHelper
    {
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudTableClient tableClient;
        private CloudQueueClient queueClient;

        public string StorageConnectionString
        {
            set
            {
                this.storageAccount = CloudStorageAccount.Parse(value);
                this.blobClient = storageAccount.CreateCloudBlobClient();
                //this.tableClient = storageAccount.CreateCloudTableClient();
                this.queueClient = storageAccount.CreateCloudQueueClient();
            }
        }

        public string TableConnectionString
        {
            set
            {
                var sa = CloudStorageAccount.Parse(value);

                this.tableClient = storageAccount.CreateCloudTableClient();

            }
        }

        public async Task<CloudBlobContainer> CreateContainerIfNotExistsAsync(string containerName)
        {
            var container = blobClient.GetContainerReference(containerName);
            BlobContainerPermissions permissions = new BlobContainerPermissions()
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };
            await container.SetPermissionsAsync(permissions);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        public async Task<string> UploadFileAsync(string imagePath, string containerName)
        {

            var fileName = Path.GetFileName(imagePath);
            var container = await CreateContainerIfNotExistsAsync(containerName);
            var blob = container.GetBlockBlobReference(fileName);//create empty file on cloud
            await blob.UploadFromFileAsync(imagePath);//then upload from local to cloud
            return blob.Uri.AbsoluteUri;
        }

        public async Task<CloudTable> CreateTableIfNotExistsAsync(string tableName)
        {
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<Customer> SaveInsuranceDetailAsync(Customer customer, string tableName)
        {
            TableOperation tableOperation = TableOperation.InsertOrMerge(customer);
            var table = await CreateTableIfNotExistsAsync(tableName);
            TableResult entity = await table.ExecuteAsync(tableOperation);
            return entity.Result as Customer;
        }

        public async Task<CloudQueue> CreateQueueIfNotExistsAsync(string queueName)
        {
            var queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync();
            return queue;
        }

        public async Task<bool> SendMessageAsync(string messageText, string queueName)
        {
            var queue = await CreateQueueIfNotExistsAsync(queueName);

            CloudQueueMessage message = new CloudQueueMessage(messageText);
            await queue.AddMessageAsync(message, TimeSpan.FromMinutes(30), TimeSpan.Zero, null, null);
            return true;
        }

    }
}
