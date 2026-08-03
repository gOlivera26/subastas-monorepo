using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using PortalSubastas.Providers.Application.Services.Interfaces;

namespace PortalSubastas.Providers.Application.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public FileStorageService(IConfiguration config)
    {
        var accessKey = config["CloudflareR2:AccessKey"] ?? throw new ArgumentNullException("AccessKey R2 faltante");
        var secretKey = config["CloudflareR2:SecretKey"] ?? throw new ArgumentNullException("SecretKey R2 faltante");
        var accountId = config["CloudflareR2:AccountId"] ?? throw new ArgumentNullException("AccountId R2 faltante");
        _bucketName = config["CloudflareR2:BucketName"] ?? throw new ArgumentNullException("BucketName R2 faltante");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        // Generamos un nombre único y lo guardamos en la carpeta constancias-afip
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

        await _s3Client.PutObjectAsync(putRequest);

        // Guardamos la ruta (Key) en la base de datos
        return uniqueFileName;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            // fileUrl puede ser la URL completa o solo el Key. 
            // Si es URL, extraemos el Key.
            var key = fileUrl.Contains("http") 
                ? new Uri(fileUrl).AbsolutePath.TrimStart('/') 
                : fileUrl;

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
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

    public async Task<Stream> DownloadFileAsync(string fileUrl)
    {
        var key = fileUrl.Contains("http") 
            ? new Uri(fileUrl).AbsolutePath.TrimStart('/') 
            : fileUrl;

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }
}
