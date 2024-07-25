using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon;
using System;
using System.Threading.Tasks;

public class S3Helper
{
    private readonly AmazonS3Client s3Client;

    public S3Helper(string region)
    {
        
        s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
    }


    public async Task UploadFileAsync(string filePath)
    {
        var fileTransferUtility = new TransferUtility(s3Client);

        await fileTransferUtility.UploadAsync(filePath, "ernzcnltn");
    }


}