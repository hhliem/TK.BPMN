using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseBpm.Form.Domain.Exceptions;

namespace EnterpriseBpm.Form.Domain.Services;

public static class FormSchemaSanitizer
{
    private static readonly Regex XssPattern = new(
        @"<\s*script[^>]*>.*?<\s*/\s*script\s*>|javascript:|onerror\s*=|onload\s*=|onclick\s*=", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Làm sạch Schema JSON của Form.io trước khi lưu vào Database, chống Stored XSS.
    /// </summary>
    public static string Sanitize(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            throw new InvalidFormSchemaException("Schema JSON của Form.io không được để trống.");
        }

        // 1. Kiểm tra tính hợp lệ cú pháp JSON
        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidFormSchemaException($"Cấu trúc JSON không hợp lệ: {ex.Message}");
        }

        // 2. Chặn các đoạn mã kịch bản XSS nguy hiểm
        if (XssPattern.IsMatch(schemaJson))
        {
            throw new InvalidFormSchemaException("Phát hiện đoạn mã script nguy hại không được phép trong Form Schema.");
        }

        return schemaJson;
    }
}

public static class AntiTamperingItemValidator
{
    /// <summary>
    /// Thẩm định tính hợp lệ quan hệ Cha - Con của dữ liệu phụ thuộc (Cascading) được nộp lên từ Client.
    /// </summary>
    public static void Validate(Guid childItemId, Guid parentItemId, Func<Guid, Guid?> getParentOf)
    {
        var actualParentId = getParentOf(childItemId);
        if (actualParentId == null || actualParentId != parentItemId)
        {
            throw new TamperedDataException(childItemId, parentItemId);
        }
    }
}

