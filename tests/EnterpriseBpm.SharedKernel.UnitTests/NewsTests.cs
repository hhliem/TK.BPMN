using EnterpriseBpm.InternalNews.Domain.Entities;
using EnterpriseBpm.InternalNews.Domain.Enums;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class NewsTests
{
    [Fact]
    public void NewsArticle_Creation_ShouldDefaultToDraftStatus()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var article = new NewsArticle(
            title: "Chính Sách Làm Việc Từ Xa 2027",
            summary: "Quy định khung giờ làm việc linh hoạt",
            contentHtml: "<p>Nội dung chi tiết...</p>",
            category: NewsCategory.Policy,
            authorId: authorId,
            isMandatoryRead: true
        );

        // Assert
        Assert.Equal(NewsStatus.Draft, article.Status);
        Assert.Equal("Chính Sách Làm Việc Từ Xa 2027", article.Title);
        Assert.True(article.IsMandatoryRead);
        Assert.Null(article.PublishedAt);
        Assert.Null(article.ArchivedAt);
    }

    [Fact]
    public void NewsArticle_Publish_ShouldUpdateStatusAndPublishedAt()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var publisherId = Guid.NewGuid();
        var article = new NewsArticle(
            title: "Thông Báo Khẩn Cấp Bão Lũ",
            summary: "Hướng dẫn sơ tán an toàn",
            contentHtml: "<p>Sơ tán tầng 1</p>",
            category: NewsCategory.Urgent,
            authorId: authorId
        );

        // Act
        article.Publish(publisherId);

        // Assert
        Assert.Equal(NewsStatus.Published, article.Status);
        Assert.NotNull(article.PublishedAt);
        Assert.Equal(publisherId, article.UpdatedBy);
    }

    [Fact]
    public void NewsArticle_Archive_ShouldUpdateStatusAndArchivedAt()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var article = new NewsArticle(
            title: "Hội Thao Mùa Thu 2026",
            summary: "Kết quả thi đấu bóng đá",
            contentHtml: "<p>Đội A vô địch</p>",
            category: NewsCategory.General,
            authorId: authorId
        );
        article.Publish(authorId);

        // Act
        article.Archive(authorId);

        // Assert
        Assert.Equal(NewsStatus.Archived, article.Status);
        Assert.NotNull(article.ArchivedAt);
    }

    [Fact]
    public void NewsReadReceipt_ShouldRecordEmployeeAndTimestamp()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        // Act
        var receipt = new NewsReadReceipt(articleId, employeeId);

        // Assert
        Assert.Equal(articleId, receipt.ArticleId);
        Assert.Equal(employeeId, receipt.EmployeeId);
        Assert.True(receipt.ReadAt <= DateTime.UtcNow);
    }
}

