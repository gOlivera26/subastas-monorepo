using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class CloudflareR2StorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _publicDomain;

    public CloudflareR2StorageService(IConfiguration config)
    {
        var accessKey = config["CloudflareR2:AccessKey"] ?? throw new ArgumentNullException("AccessKey R2 faltante");
        var secretKey = config["CloudflareR2:SecretKey"] ?? throw new ArgumentNullException("SecretKey R2 faltante");
        var accountId = config["CloudflareR2:AccountId"] ?? throw new ArgumentNullException("AccountId R2 faltante");
        _publicDomain = config["CloudflareR2:PublicDomain"] ?? throw new ArgumentNullException("PublicDomain R2 faltante");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string bucketName, string prefix = "")
    {
        var fileExtension = Path.GetExtension(file.FileName).ToLower();

        if (fileExtension != ".pdf" && fileExtension != ".png" && fileExtension != ".jpg")
            throw new ArgumentException("Formato de archivo no permitido.");

        var fileName = $"{prefix}{Guid.NewGuid()}{fileExtension}";

        using var stream = file.OpenReadStream();
        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = file.ContentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(putRequest);

        return $"{_publicDomain}/{fileName}";
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, string bucketName)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            return true;
        }
        catch
        {
            return false;
        }
    }
}