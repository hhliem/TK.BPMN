namespace EnterpriseBpm.Org.Domain.Exceptions;

public class CircularOrgException : Exception
{
    public Guid OrgUnitId { get; }
    public Guid ProposedParentId { get; }

    public CircularOrgException(Guid orgUnitId, Guid proposedParentId)
        : base($"Không thể gán đơn vị '{proposedParentId}' làm cấp trên của '{orgUnitId}' vì sẽ tạo ra vòng lặp vô tận trong cây tổ chức phân cấp.")
    {
        OrgUnitId = orgUnitId;
        ProposedParentId = proposedParentId;
    }
}

public class InvalidDelegationException : Exception
{
    public Guid DelegatorId { get; }
    public Guid DelegateeId { get; }

    public InvalidDelegationException(Guid delegatorId, Guid delegateeId, string reason)
        : base($"Ủy quyền từ '{delegatorId}' sang '{delegateeId}' không hợp lệ: {reason}")
    {
        DelegatorId = delegatorId;
        DelegateeId = delegateeId;
    }
}

