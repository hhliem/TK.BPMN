namespace EnterpriseBpm.AiAssistant.Domain.Enums;

public enum AiProviderType
{
    Gemini = 1,
    OpenAi = 2,
    Claude = 3,
    Ollama = 4,
    CustomMcp = 5
}

public enum ChatRole
{
    System = 1,
    User = 2,
    Assistant = 3,
    Tool = 4
}

public enum KnowledgeDocumentType
{
    Policy = 1,      // Quy chế nội bộ
    Procedure = 2,   // Quy trình nghiệp vụ
    Circular = 3,    // Thông tư / Hướng dẫn
    Regulation = 4,  // Nội quy lao động / kỷ luật
    General = 5      // Tài liệu chung
}

public enum IndexingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

