using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon;
using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using System.Collections.Generic;
using System.IO;

public class S3Helper
{
    private readonly IAmazonS3 s3Client;
    private readonly string bucketName;

    public S3Helper(string region, string bucketName)
    {
        this.bucketName = bucketName;
        s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
    }

    public async Task UploadFileAsync(string filePath, string userPrefix)
    {
        var fileTransferUtility = new TransferUtility(s3Client);
        string key = $"{userPrefix}/{Path.GetFileName(filePath)}";
        await fileTransferUtility.UploadAsync(filePath, bucketName, key);
    }

    public async Task DownloadFileAsync(string fileName, string destinationPath, string userPrefix)
    {
        var fileTransferUtility = new TransferUtility(s3Client);
        string key = $"{userPrefix}/{(fileName)}";
        await fileTransferUtility.DownloadAsync(destinationPath, bucketName, key);
    }

    public async Task<List<S3Object>> ListFilesAsync(string userPrefix)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = $"{userPrefix}/"
        };

        var response = await s3Client.ListObjectsV2Async(request);

        return response.S3Objects;
    }

    public async Task EnsureUserFolderExistsAsync(string userPrefix)
    {
        string tempFile = Path.GetTempFileName();
        await UploadFileAsync(tempFile, userPrefix);
        File.Delete(tempFile);
    }
}