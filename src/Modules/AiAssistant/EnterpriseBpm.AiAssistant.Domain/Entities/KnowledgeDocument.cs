using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.AiAssistant.Domain.Entities;

public class KnowledgeDocument : BaseEntity<Guid>, IAuditable
{
    public string Title { get; private set; } = default!;
    public KnowledgeDocumentType DocumentType { get; private set; } = KnowledgeDocumentType.Policy;
    public string SourceFilePath { get; private set; } = default!;
    public string QdrantCollectionName { get; private set; } = "company_policies";
    public int ChunkCount { get; private set; } = 0;
    public string EmbeddingModel { get; private set; } = "nomic-embed-text";
    public IndexingStatus Status { get; private set; } = IndexingStatus.Pending;
    public string? ErrorMessage { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime IndexedAt { get; private set; } = DateTime.UtcNow;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected KnowledgeDocument() { }

    public static KnowledgeDocument Create(
        string title,
        KnowledgeDocumentType documentType,
        string sourceFilePath,
        string qdrantCollectionName = "company_policies",
        string embeddingModel = "nomic-embed-text",
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề tài liệu tri thức không được để trống.", nameof(title));
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Đường dẫn tệp nguồn không được để trống.", nameof(sourceFilePath));

        return new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            DocumentType = documentType,
            SourceFilePath = sourceFilePath.Trim(),
            QdrantCollectionName = string.IsNullOrWhiteSpace(qdrantCollectionName) ? "company_policies" : qdrantCollectionName.Trim(),
            EmbeddingModel = string.IsNullOrWhiteSpace(embeddingModel) ? "nomic-embed-text" : embeddingModel.Trim(),
            Status = IndexingStatus.Pending,
            IsActive = true,
            IndexedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void MarkProcessing()
    {
        Status = IndexingStatus.Processing;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted(int chunkCount)
    {
        Status = IndexingStatus.Completed;
        ChunkCount = chunkCount;
        ErrorMessage = null;
        IndexedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = IndexingStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

