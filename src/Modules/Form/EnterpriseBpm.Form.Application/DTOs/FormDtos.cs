namespace EnterpriseBpm.Form.Application.DTOs;

public record FormDefinitionDto(
    Guid Id,
    string FormKey,
    string Title,
    string SchemaJson,
    int Version,
    bool IsLatest,
    string? ChangeNotes,
    Guid? PublishedBy,
    DateTime CreatedAt
);

public record FormVersionHistoryDto(
    Guid Id,
    int Version,
    bool IsLatest,
    string? ChangeNotes,
    Guid? PublishedBy,
    DateTime CreatedAt
);

public record RegisterFormCommand(
    string FormKey,
    string Title,
    string SchemaJson
);

public record MasterItemDto(
    Guid Id,
    Guid CategoryId,
    Guid? ParentId,
    string Code,
    string Name,
    int DisplayOrder,
    string? CustomAttributesJson
);

public record CreateMasterItemCommand(
    string CategoryKey,
    Guid? ParentId,
    string Code,
    string Name,
    int DisplayOrder,
    string? CustomAttributesJson
);

