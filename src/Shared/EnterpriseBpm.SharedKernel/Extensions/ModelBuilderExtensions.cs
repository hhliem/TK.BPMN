using System.Linq.Expressions;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.SharedKernel.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Tự động áp dụng Global Query Filter [DeletedAt == null] cho toàn bộ Entity thuộc ISoftDeletable
    /// </summary>
    public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}

