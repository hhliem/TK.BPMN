using EnterpriseBpm.Document.Domain.Entities;
using EnterpriseBpm.Document.Domain.Enums;
using EnterpriseBpm.Document.Domain.Exceptions;
using EnterpriseBpm.Document.Domain.Services;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class DocumentDomainTests
{
    [Theory]
    [InlineData("hop-dong-kinh-te.pdf", 1024 * 1024, "application/pdf")]
    [InlineData("bang-ke-chi-phi.xlsx", 500 * 1024, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("anh-hien-truong.jpg", 2 * 1024 * 1024, "image/jpeg")]
    [InlineData("to-trinh-mua-sam.docx", 800 * 1024, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void FileValidationRules_ShouldAccept_ValidFiles(string fileName, long size, string contentType)
    {
        // Act & Assert - Không ném ngoại lệ
        FileValidationRules.Validate(fileName, size, contentType);
    }

    [Theory]
    [InlineData("trojan.exe", 1024)]
    [InlineData("script.sh", 2048)]
    [InlineData("batch.bat", 512)]
    [InlineData("macro.vbs", 100)]
    public void FileValidationRules_ShouldReject_DisallowedExtensions(string fileName, long size)
    {
        // Act & Assert
        Assert.Throws<InvalidFileException>(() => FileValidationRules.Validate(fileName, size));
    }

    [Fact]
    public void FileValidationRules_ShouldReject_EmptyFiles()
    {
        // Act & Assert
        Assert.Throws<InvalidFileException>(() => FileValidationRules.Validate("empty.pdf", 0));
    }

    [Fact]
    public void FileValidationRules_ShouldReject_FilesExceeding25MB()
    {
        // Arrange - 26 MB
        var size26Mb = 26L * 1024 * 1024;

        // Act & Assert
        Assert.Throws<FileTooLargeException>(() => FileValidationRules.Validate("large-video.pdf", size26Mb));
    }

    [Fact]
    public void WorkflowAttachment_SoftDelete_ShouldSetAuditFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var attachment = WorkflowAttachment.Create(
            workflowInstanceId: "wf_123456",
            formKey: "purchase_request",
            originalFileName: "bao-gia.pdf",
            contentType: "application/pdf",
            fileSizeBytes: 1024 * 100,
            oneDriveItemId: "item_001",
            oneDriveWebUrl: "https://onedrive.mock/item_001",
            uploadedByUserId: userId);

        Assert.Null(attachment.DeletedAt);
        Assert.Null(attachment.DeletedBy);

        // Act
        var deletedBy = Guid.NewGuid();
        attachment.MarkDeleted(deletedBy);

        // Assert
        Assert.NotNull(attachment.DeletedAt);
        Assert.Equal(deletedBy, attachment.DeletedBy);
    }

    [Fact]
    public void GeneratedDocument_ShouldStore_Sha256IntegrityHash()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var expectedHash = "a1b2c3d4e5f678901234567890abcdef1234567890abcdef1234567890abcdef";
        var userId = Guid.NewGuid();

        // Act
        var doc = GeneratedDocument.Create(
            templateId: templateId,
            workflowInstanceId: "wf_approval_789",
            generatedFileName: "QuyetDinhPheDuyet_001.pdf",
            oneDriveItemId: "drv_item_999",
            oneDriveWebUrl: "https://onedrive.mock/doc_999",
            contentSha256Hash: expectedHash,
            generatedByUserId: userId);

        // Assert
        Assert.Equal(expectedHash, doc.ContentSha256Hash);
        Assert.Equal("wf_approval_789", doc.WorkflowInstanceId);
        Assert.Equal(templateId, doc.TemplateId);
    }
}

