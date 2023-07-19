using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicensePlateProcessingFunctions.StorageAndFileProcessing
{
	public class BlobStorageHelper
	{
		private readonly string _containerName;
		private readonly string _blobStorageConnection;
		private readonly ILogger _log;

		public BlobStorageHelper(string connectionString, string containerName, ILogger log)
		{
			_blobStorageConnection = connectionString;
			_containerName = containerName;
			_log = log;
		}

		public async Task<bool> UploadBlob(byte[] blobBytes, string fileName)
		{
			var ms = new MemoryStream(blobBytes);

			// Create a BlobServiceClient object which will be used to create a container client
			var blobServiceClient = new BlobServiceClient(_blobStorageConnection);

			// Create the container and return a container client object
			var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
			var blobClient = containerClient.GetBlobClient(fileName);

			_log.LogInformation($"Uploading to Blob storage as blob:{Environment.NewLine}{blobClient.Uri}\n");

			// Upload data from the stream
			var result = await blobClient.UploadAsync(ms, true);
			var success = result.GetRawResponse().Status == 201;
			_log.LogInformation($"Successful Upload: {success}");
			return success;
		}

		public async Task<byte[]> DownloadBlob(string fileName)
		{
			// Create a BlobServiceClient object which will be used to create a container client
			var blobServiceClient = new BlobServiceClient(_blobStorageConnection);

			// Create the container and return a container client object
			var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
			var blobClient = containerClient.GetBlobClient(fileName);

			_log.LogInformation($"Downloading blob as byte array from {blobClient.Uri}");

			// Download the blob's contents and save it to a stream
			var stream = new MemoryStream();
			await blobClient.DownloadToAsync(stream);
			stream.Position = 0;
			_log.LogInformation($"{stream.Length} bytes received");
			return stream.ToArray();
		}
	}
}
