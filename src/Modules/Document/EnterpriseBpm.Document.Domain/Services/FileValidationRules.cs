using EnterpriseBpm.Document.Domain.Exceptions;

namespace EnterpriseBpm.Document.Domain.Services;

public static class FileValidationRules
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".xlsx",
        ".pptx",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/jpeg",
        "image/png"
    };

    public static void Validate(string fileName, long sizeBytes, string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidFileException("Tên tệp không được để trống.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidFileException(
                $"Định dạng tệp '{extension}' không nằm trong danh sách được phép ({string.Join(", ", AllowedExtensions)}).");
        }

        if (sizeBytes <= 0)
        {
            throw new InvalidFileException("Tệp tải lên không có dữ liệu (kích thước 0 byte).");
        }

        if (sizeBytes > MaxFileSizeBytes)
        {
            throw new FileTooLargeException(sizeBytes, MaxFileSizeBytes);
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedContentTypes.Contains(contentType))
        {
            // Cảnh báo nếu Content-Type không khớp whitelist chuẩn
            // Một số browser gửi application/octet-stream cho một số file, nếu extension chuẩn thì chấp nhận
            if (contentType != "application/octet-stream")
            {
                throw new InvalidFileException($"Loại nội dung (MIME Type) '{contentType}' không được chấp thuận.");
            }
        }
    }
}

