using ClosedXML.Excel;
using EnterpriseBpm.AuditLog.Application.Contracts;
using EnterpriseBpm.AuditLog.Application.DTOs;
using EnterpriseBpm.AuditLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.AuditLog.Infrastructure.Services;

public class AuditQueryService : IAuditQueryService
{
    private readonly AuditDbContext _context;

    public AuditQueryService(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditEntryDto>> GetEntityHistoryAsync(
        string entityType, 
        string entityId, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditEntries
            .AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEntryDto(
                e.Id,
                e.Timestamp,
                e.UserId,
                e.UserName,
                e.Action,
                e.EntityType,
                e.EntityId,
                e.ModuleName,
                e.OldValues,
                e.NewValues,
                e.IpAddress,
                e.UserAgent))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryDto>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<PagedResult<AuditEntryDto>> GetUserActivityAsync(
        Guid userId, 
        DateTime? from, 
        DateTime? to, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        if (from.HasValue) query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Timestamp <= to.Value);

        query = query.OrderByDescending(e => e.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEntryDto(
                e.Id,
                e.Timestamp,
                e.UserId,
                e.UserName,
                e.Action,
                e.EntityType,
                e.EntityId,
                e.ModuleName,
                e.OldValues,
                e.NewValues,
                e.IpAddress,
                e.UserAgent))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryDto>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<PagedResult<SecurityEventDto>> GetSecurityEventsAsync(
        DateTime? from, 
        DateTime? to, 
        string? eventType, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityEvents.AsNoTracking();

        if (from.HasValue) query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Timestamp <= to.Value);
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);

        query = query.OrderByDescending(e => e.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new SecurityEventDto(
                e.Id,
                e.Timestamp,
                e.UserId,
                e.UserName,
                e.EventType,
                e.IpAddress,
                e.IsSuccess,
                e.FailureReason))
            .ToListAsync(cancellationToken);

        return new PagedResult<SecurityEventDto>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<byte[]> ExportExcelAsync(
        DateTime from, 
        DateTime to, 
        string? moduleName, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditEntries
            .AsNoTracking()
            .Where(e => e.Timestamp >= from && e.Timestamp <= to);

        if (!string.IsNullOrWhiteSpace(moduleName))
            query = query.Where(e => e.ModuleName == moduleName);

        var entries = await query
            .OrderByDescending(e => e.Timestamp)
            .Take(5000)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Logs");

        // Header
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Thời gian (UTC)";
        worksheet.Cell(1, 3).Value = "Người dùng";
        worksheet.Cell(1, 4).Value = "Hành động";
        worksheet.Cell(1, 5).Value = "Phân hệ";
        worksheet.Cell(1, 6).Value = "Thực thể";
        worksheet.Cell(1, 7).Value = "Mã thực thể";
        worksheet.Cell(1, 8).Value = "IP Address";
        worksheet.Cell(1, 9).Value = "Giá trị Cũ (OldValues)";
        worksheet.Cell(1, 10).Value = "Giá trị Mới (NewValues)";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.Navy;
        headerRow.Style.Font.FontColor = XLColor.White;

        // Data
        for (int i = 0; i < entries.Count; i++)
        {
            var row = i + 2;
            var e = entries[i];
            worksheet.Cell(row, 1).Value = e.Id;
            worksheet.Cell(row, 2).Value = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(row, 3).Value = e.UserName;
            worksheet.Cell(row, 4).Value = e.Action;
            worksheet.Cell(row, 5).Value = e.ModuleName;
            worksheet.Cell(row, 6).Value = e.EntityType;
            worksheet.Cell(row, 7).Value = e.EntityId;
            worksheet.Cell(row, 8).Value = e.IpAddress ?? "-";
            worksheet.Cell(row, 9).Value = e.OldValues ?? "-";
            worksheet.Cell(row, 10).Value = e.NewValues ?? "-";
        }

        worksheet.Columns().AdjustToContents(1, 50);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

