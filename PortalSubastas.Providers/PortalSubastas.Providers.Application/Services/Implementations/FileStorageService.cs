using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using PortalSubastas.Providers.Application.Services.Interfaces;

namespace PortalSubastas.Providers.Application.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly string? _accessKey;
    private readonly string? _secretKey;
    private readonly string? _accountId;
    private readonly string? _bucketName;

    public FileStorageService(IConfiguration config)
    {
        _accessKey = config["CloudflareR2:AccessKey"];
        _secretKey = config["CloudflareR2:SecretKey"];
        _accountId = config["CloudflareR2:AccountId"];
        _bucketName = config["CloudflareR2:BucketName"];
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var s3Client = CreateClient();
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"constancias-afip/{Guid.NewGuid()}{extension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = uniqueFileName,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(putRequest);
        return uniqueFileName;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var s3Client = CreateClient();
            var key = fileUrl.Contains("http")
                ? new Uri(fileUrl).AbsolutePath.TrimStart('/')
                : fileUrl;

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await s3Client.DeleteObjectAsync(deleteRequest);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stream> DownloadFileAsync(string fileUrl)
    {
        var s3Client = CreateClient();
        var key = fileUrl.Contains("http")
            ? new Uri(fileUrl).AbsolutePath.TrimStart('/')
            : fileUrl;

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        var response = await s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    private IAmazonS3 CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_accessKey) ||
            string.IsNullOrWhiteSpace(_secretKey) ||
            string.IsNullOrWhiteSpace(_accountId) ||
            string.IsNullOrWhiteSpace(_bucketName))
        {
            throw new InvalidOperationException("El almacenamiento documental de proveedores no está configurado.");
        }

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{_accountId}.r2.cloudflarestorage.com",
        };

        return new AmazonS3Client(_accessKey, _secretKey, s3Config);
    }
}
