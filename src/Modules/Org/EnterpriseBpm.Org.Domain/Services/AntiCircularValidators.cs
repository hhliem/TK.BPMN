using EnterpriseBpm.Org.Domain.Exceptions;

namespace EnterpriseBpm.Org.Domain.Services;

public static class AntiCircularAncestryValidator
{
    /// <summary>
    /// Kiểm tra ngăn chặn vòng lặp phân cấp khi thay đổi ParentId của một Đơn vị tổ chức.
    /// Thuật toán duyệt ngược từ newParentId lên đỉnh cây (Ancestry Traversal).
    /// </summary>
    public static void ValidateNoCycle(
        Guid currentUnitId, 
        Guid? newParentId, 
        Func<Guid, Guid?> getParentOf)
    {
        if (!newParentId.HasValue)
        {
            return; // Đặt làm Root, không bao giờ tạo vòng lặp
        }

        if (currentUnitId == newParentId.Value)
        {
            throw new CircularOrgException(currentUnitId, newParentId.Value);
        }

        var visited = new HashSet<Guid> { currentUnitId };
        var checkId = newParentId;

        while (checkId.HasValue)
        {
            if (visited.Contains(checkId.Value))
            {
                throw new CircularOrgException(currentUnitId, newParentId.Value);
            }

            visited.Add(checkId.Value);
            checkId = getParentOf(checkId.Value);
        }
    }
}

public static class AntiCircularDelegationValidator
{
    /// <summary>
    /// Thẩm định ủy quyền hợp lệ: chống tự ủy quyền và chống ủy quyền chéo/vòng tròn trong cùng khung thời gian.
    /// </summary>
    public static void Validate(
        Guid delegatorId, 
        Guid delegateeId, 
        DateTime fromDate, 
        DateTime toDate, 
        IEnumerable<(Guid DelegatorId, Guid DelegateeId, DateTime FromDate, DateTime ToDate, bool IsActive)> existingActiveDelegations)
    {
        if (delegatorId == delegateeId)
        {
            throw new InvalidDelegationException(delegatorId, delegateeId, "Không thể tự ủy quyền cho chính mình.");
        }

        if (fromDate >= toDate)
        {
            throw new InvalidDelegationException(delegatorId, delegateeId, "Thời điểm bắt đầu ủy quyền phải trước thời điểm kết thúc.");
        }

        // Kiểm tra xem Delegatee có đang ủy quyền ngược lại cho Delegator trong khoảng thời gian này không
        foreach (var d in existingActiveDelegations)
        {
            if (d.IsActive && d.DelegatorId == delegateeId && d.DelegateeId == delegatorId)
            {
                // Kiểm tra chồng lấn thời gian (Date overlap)
                var hasOverlap = fromDate <= d.ToDate && toDate >= d.FromDate;
                if (hasOverlap)
                {
                    throw new InvalidDelegationException(
                        delegatorId, 
                        delegateeId, 
                        $"Người được ủy quyền hiện đang ủy quyền ngược lại cho bạn từ {d.FromDate:dd/MM/yyyy} đến {d.ToDate:dd/MM/yyyy}. Hành động này sẽ tạo thành vòng lặp phê duyệt.");
                }
            }
        }
    }
}

