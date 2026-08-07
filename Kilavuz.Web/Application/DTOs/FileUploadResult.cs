namespace Kilavuz.Web.Application.DTOs;

public record FileUploadResult(
    string OriginalFileName,
    string StoredFileName,
    long FileSize,
    string ContentType,
    string RelativePath
);
