using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Kilavuz.Web.Application.DTOs;

namespace Kilavuz.Web.Application.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> UploadImageAsync(IFormFile file);
    Task<FileUploadResult> UploadAttachmentAsync(IFormFile file);
}
