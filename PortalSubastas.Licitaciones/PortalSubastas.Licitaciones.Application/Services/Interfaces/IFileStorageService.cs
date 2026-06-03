using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string bucketName, string prefix = "");
    Task<bool> DeleteFileAsync(string fileUrl, string bucketName);
}
