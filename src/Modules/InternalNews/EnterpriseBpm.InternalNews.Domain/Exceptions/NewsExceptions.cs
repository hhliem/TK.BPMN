namespace EnterpriseBpm.InternalNews.Domain.Exceptions;

public class NewsArticleNotFoundException : Exception
{
    public Guid ArticleId { get; }

    public NewsArticleNotFoundException(Guid articleId)
        : base($"Không tìm thấy bài viết tin tức với định danh: '{articleId}'.")
    {
        ArticleId = articleId;
    }
}

public class InvalidNewsOperationException : Exception
{
    public InvalidNewsOperationException(string message) : base(message)
    {
    }
}

