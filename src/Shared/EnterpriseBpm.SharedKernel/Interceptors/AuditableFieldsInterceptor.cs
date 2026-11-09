using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnterpriseBpm.SharedKernel.Interceptors;

/// <summary>
/// EF Core SaveChangesInterceptor tự động gán CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
/// </summary>
public class AuditableFieldsInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableFieldsInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return ValueTask.FromResult(result);

        var utcNow = DateTime.UtcNow;
        var currentUserId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy ??= currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }

        // Xử lý Soft Delete tự động nếu entity có ISoftDeletable mà đang bị xóa EntityState.Deleted
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = utcNow;
                entry.Entity.DeletedBy = currentUserId;
            }
        }

        return ValueTask.FromResult(result);
    }
}

