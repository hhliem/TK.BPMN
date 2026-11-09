namespace EnterpriseBpm.SharedKernel;

/// <summary>
/// Base Entity định danh với khóa chính GUID chuẩn
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Generic Base Entity cho các Entity có kiểu khóa chính tùy biến
/// </summary>
public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (EqualityComparer<TId>.Default.Equals(Id, default))
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.Equals(Id, default) 
            ? base.GetHashCode() 
            : EqualityComparer<TId>.Default.GetHashCode(Id!);
    }

    public static bool operator ==(BaseEntity<TId>? a, BaseEntity<TId>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(BaseEntity<TId>? a, BaseEntity<TId>? b) => !(a == b);
}

