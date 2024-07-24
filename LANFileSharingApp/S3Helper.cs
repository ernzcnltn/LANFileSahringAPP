using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon;
using System;
using System.Threading.Tasks;

public class S3Helper
{
    private readonly IAmazonS3 s3Client;
    private readonly string bucketName = "ernzcnltn";

    public S3Helper(string accessKey, string secretKey, string region)
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task UploadFileAsync(string filePath)
    {
        try
        {
            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(filePath, bucketName);
            Console.WriteLine("File uploaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
        }
    }

   
}