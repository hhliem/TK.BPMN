using System.Text.Json;
using EnterpriseBpm.EventBus.Outbox;
using EnterpriseBpm.SharedKernel.Caching;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SharedKernel.Logging;
using EnterpriseBpm.SharedKernel.Resilience;
using EnterpriseBpm.SharedKernel.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

/// <summary>
/// Bộ kiểm thử Unit Tests cho Sprint 08:
/// High-Performance Tuning, Resilience Engineering & Production Go-Live
/// (TSK-801, TSK-802, TSK-803, TSK-804, TSK-809, TSK-810)
/// </summary>
public class Sprint08Tests
{
    // =========================================================================
    // TSK-802 / TSK-803: Polly v8 Resilience Pipeline & Circuit Breaker Tests
    // =========================================================================

    [Fact]
    public async Task CircuitBreaker_InitialState_ShouldBeClosed_AndExecuteSuccessfully()
    {
        // Arrange
        var breaker = new CircuitBreakerPolicy(
            name: "TestService",
            failureThresholdRate: 0.5,
            samplingDuration: TimeSpan.FromSeconds(10),
            breakDuration: TimeSpan.FromSeconds(2),
            minimumThroughput: 2);

        Assert.Equal(CircuitState.Closed, breaker.State);

        // Act
        var result = await breaker.ExecuteAsync(async () =>
        {
            await Task.Yield();
            return "OK-Success";
        });

        // Assert
        Assert.Equal("OK-Success", result);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_WhenFailureRateExceedsThreshold_ShouldTripToOpen_AndFailFast_GherkinScenario()
    {
        // Gherkin AC:
        // Given Microsoft Graph API or External API returns 503 errors with failure rate >= 50%
        // When consecutive requests are executed
        // Then Circuit Breaker transitions to "Open"
        // And subsequent requests throw CircuitBreakerOpenException immediately without calling the action.

        // Arrange
        var breaker = new CircuitBreakerPolicy(
            name: "GraphApiBreaker",
            failureThresholdRate: 0.5,
            samplingDuration: TimeSpan.FromSeconds(10),
            breakDuration: TimeSpan.FromMilliseconds(500),
            minimumThroughput: 2);

        // Act 1: Gây ra 2 lỗi liên tiếp (2/2 = 100% failure rate >= 50% threshold)
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await breaker.ExecuteAsync<string>(() => throw new HttpRequestException("503 Service Unavailable"));
            });
        }

        // Assert 1: Circuit breaker đã chuyển sang Open
        Assert.Equal(CircuitState.Open, breaker.State);

        // Act 2: Request tiếp theo phải fail-fast ngay lập tức với CircuitBreakerOpenException
        var executedUnderlyingAction = false;
        var ex = await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
        {
            await breaker.ExecuteAsync(async () =>
            {
                executedUnderlyingAction = true;
                await Task.Yield();
                return "Should not reach here";
            });
        });

        // Assert 2: Không được gọi underlying action (bảo vệ thread pool khỏi cạn kiệt)
        Assert.False(executedUnderlyingAction);
        Assert.Contains("trạng thái OPEN", ex.Message);
    }

    [Fact]
    public async Task CircuitBreaker_AfterBreakDuration_ShouldTransitionToHalfOpen_AndResetOnSuccess()
    {
        // Arrange
        var breakDuration = TimeSpan.FromMilliseconds(150);
        var breaker = new CircuitBreakerPolicy(
            name: "ProbeService",
            failureThresholdRate: 0.5,
            samplingDuration: TimeSpan.FromSeconds(10),
            breakDuration: breakDuration,
            minimumThroughput: 2);

        // Gây lỗi để trip sang Open
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await breaker.ExecuteAsync<string>(() => throw new InvalidOperationException("External Error"));
            });
        }
        Assert.Equal(CircuitState.Open, breaker.State);

        // Chờ hết thời gian break
        await Task.Delay(200);

        // Act: Request đầu tiên sau thời gian break sẽ probe (HalfOpen -> Closed nếu thành công)
        var recovered = await breaker.ExecuteAsync(async () =>
        {
            await Task.Yield();
            return "Recovered-Successfully";
        });

        // Assert
        Assert.Equal("Recovered-Successfully", recovered);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    // =========================================================================
    // TSK-804: Multi-Level Caching (L1 In-Memory + L2 Distributed) Tests
    // =========================================================================

    [Fact]
    public async Task MultiLevelCache_WhenL1AndL2Miss_ShouldExecuteFactory_AndPopulateBothCaches()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new TestInMemoryDistributedCache();
        var cacheService = new MultiLevelCacheService(memoryCache, distributedCache, NullLogger<MultiLevelCacheService>.Instance);

        var cacheKey = "org:units:all";
        var factoryCallCount = 0;

        // Act 1: Lần đầu tiên gọi (L1 miss, L2 miss -> gọi Factory)
        var data1 = await cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            factoryCallCount++;
            await Task.Yield();
            return new List<string> { "BOD", "IT-DEPT", "FIN-DEPT" };
        }, l1Ttl: TimeSpan.FromMinutes(5), l2Ttl: TimeSpan.FromHours(1));

        // Assert 1
        Assert.NotNull(data1);
        Assert.Equal(3, data1.Count);
        Assert.Equal(1, factoryCallCount);

        // Act 2: Lần thứ 2 gọi ngay sau đó (L1 hit -> trả về ngay, không gọi Factory)
        var data2 = await cacheService.GetOrSetAsync<List<string>>(cacheKey, async () =>
        {
            factoryCallCount++;
            await Task.Yield();
            return new List<string> { "OTHER" };
        });

        // Assert 2
        Assert.NotNull(data2);
        Assert.Equal(3, data2.Count);
        Assert.Equal(1, factoryCallCount); // Không tăng

        // Act 3: Xóa L1 để giả lập L1 miss nhưng L2 hit
        memoryCache.Remove(cacheKey);

        var data3 = await cacheService.GetOrSetAsync<List<string>>(cacheKey, async () =>
        {
            factoryCallCount++;
            await Task.Yield();
            return new List<string> { "SHOULD_NOT_BE_CALLED" };
        });

        // Assert 3: Lấy từ L2 phục hồi sang L1, không gọi Factory
        Assert.NotNull(data3);
        Assert.Equal(3, data3.Count);
        Assert.Equal(1, factoryCallCount);
    }

    [Fact]
    public async Task MultiLevelCache_RemoveByPrefixAsync_ShouldEvictFromBothLayers()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new TestInMemoryDistributedCache();
        var cacheService = new MultiLevelCacheService(memoryCache, distributedCache, NullLogger<MultiLevelCacheService>.Instance);

        var key1 = "form:defs:payment";
        var key2 = "form:defs:leave";
        await cacheService.SetAsync(key1, "SchemaPayment");
        await cacheService.SetAsync(key2, "SchemaLeave");

        // Act: Xóa theo prefix "form:defs:"
        await cacheService.RemoveByPrefixAsync("form:defs:");

        // Assert: Cả 2 key đều không còn trong L1
        Assert.Null(await cacheService.GetAsync<string>(key1));
        Assert.Null(await cacheService.GetAsync<string>(key2));
    }

    // =========================================================================
    // TSK-809: Structured Logging & Correlation ID Tracing Middleware Tests
    // =========================================================================

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderMissing_ShouldGenerateAndAttachCorrelationId()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(
            next: (ctx) =>
            {
                Assert.True(ctx.Items.ContainsKey(CorrelationIdMiddleware.CorrelationIdHeader));
                return Task.CompletedTask;
            },
            logger: NullLogger<CorrelationIdMiddleware>.Instance);

        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderPresent_ShouldPreserveCorrelationId()
    {
        // Arrange
        var existingCorrelationId = "CORR-TEST-ABC-12345";
        var middleware = new CorrelationIdMiddleware(
            next: (ctx) => Task.CompletedTask,
            logger: NullLogger<CorrelationIdMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = existingCorrelationId;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(existingCorrelationId, context.Response.Headers["X-Correlation-ID"].ToString());
    }

    // =========================================================================
    // TSK-810: OWASP Security Headers Middleware Tests
    // =========================================================================

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldInjectAllMandatoryOwaspHeaders()
    {
        // Arrange
        var middleware = new SecurityHeadersMiddleware(next: (ctx) => Task.CompletedTask);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
        Assert.Contains("geolocation=()", context.Response.Headers["Permissions-Policy"].ToString());
    }

    // =========================================================================
    // TSK-801: Transactional Outbox Pattern Interceptor Tests
    // =========================================================================

    [Fact]
    public async Task OutboxInterceptor_WhenEntityHasDomainEvents_ShouldPersistOutboxMessageAndClearEvents()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxMessageSaveChangesInterceptor())
            .Options;

        using var db = new TestOutboxDbContext(options);

        var testEntity = new TestWorkflowAggregate
        {
            Id = Guid.NewGuid(),
            Title = "Đơn đề nghị mua sắm máy chủ",
            Status = "Approved"
        };

        // Gắn Domain Event vào Entity
        testEntity.AddDomainEvent(new TestTaskAssignedEvent(testEntity.Id, "USR-APPROVER-01", "Phê duyệt ngân sách"));
        Assert.Single(testEntity.DomainEvents);

        // Act: Save Changes
        db.TestAggregates.Add(testEntity);
        var savedCount = await db.SaveChangesAsync();

        // Assert:
        // 1. Domain Events trên entity phải được xóa sạch sau SaveChanges
        Assert.Empty(testEntity.DomainEvents);

        // 2. Bảng OutboxMessages phải có bản ghi sự kiện được serialize thành JSON
        var outboxMessages = await db.OutboxMessages.ToListAsync();
        Assert.Single(outboxMessages);

        var outboxMsg = outboxMessages.First();
        Assert.Equal(typeof(TestTaskAssignedEvent).AssemblyQualifiedName, outboxMsg.EventType);
        Assert.Null(outboxMsg.ProcessedAt);
        Assert.Equal(0, outboxMsg.RetryCount);
        Assert.Contains("USR-APPROVER-01", outboxMsg.Payload);
        Assert.Contains("Phê duyệt ngân sách", outboxMsg.Payload);
    }
}

// -----------------------------------------------------------------------------
// Test Helpers & Mock Classes
// -----------------------------------------------------------------------------

internal class TestInMemoryDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public byte[]? Get(string key) => _storage.TryGetValue(key, out var val) ? val : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _storage[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => _storage.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}

internal class TestWorkflowAggregate : EnterpriseBpm.SharedKernel.Contracts.IHasDomainEvents
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

internal record TestTaskAssignedEvent(Guid SubmissionId, string AssigneeId, string TaskName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

internal class TestOutboxDbContext : DbContext
{
    public DbSet<TestWorkflowAggregate> TestAggregates => Set<TestWorkflowAggregate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestWorkflowAggregate>().HasKey(x => x.Id);
        modelBuilder.Entity<TestWorkflowAggregate>().Ignore(x => x.DomainEvents);

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).IsRequired().HasMaxLength(250);
            b.Property(x => x.Payload).IsRequired();
        });
    }
}

