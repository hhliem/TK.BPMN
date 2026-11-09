using System.Data;
using System.Data.Common;
using System.Diagnostics;
using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.DTOs;
using EnterpriseBpm.Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Analytics.Infrastructure.Repositories;

/// <summary>
/// Repository truy vấn Big Data hiệu năng cao (Skill: High-Performance Tuning)
/// Loại bỏ hoàn toàn Change Tracking của EF Core, sử dụng DbCommand trực tiếp với SequentialAccess.
/// </summary>
public class AnalyticsDapperRepository : IAnalyticsDapperRepository
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly ILogger<AnalyticsDapperRepository> _logger;

    public AnalyticsDapperRepository(AnalyticsDbContext dbContext, ILogger<AnalyticsDapperRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DynamicReportResultDto> ExecuteDynamicQueryAsync(
        string rawSql,
        Dictionary<string, object> parameters,
        IReadOnlyList<DynamicReportColumnDto> columns,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var conn = _dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        var rows = new List<Dictionary<string, object?>>();
        int totalCount = 0;

        try
        {
            // 1. Thực thi truy vấn đếm TotalCount
            using (var countCmd = conn.CreateCommand())
            {
                var fromIdx = rawSql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
                var orderIdx = rawSql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
                var countSql = orderIdx > fromIdx
                    ? "SELECT COUNT(1) " + rawSql.Substring(fromIdx, orderIdx - fromIdx)
                    : "SELECT COUNT(1) " + rawSql.Substring(fromIdx);

                countCmd.CommandText = countSql;
                countCmd.CommandType = CommandType.Text;

                foreach (var (k, v) in parameters)
                {
                    if (k != "@offset" && k != "@limit")
                    {
                        var p = countCmd.CreateParameter();
                        p.ParameterName = k;
                        p.Value = v ?? DBNull.Value;
                        countCmd.Parameters.Add(p);
                    }
                }

                var countResult = await countCmd.ExecuteScalarAsync(cancellationToken);
                totalCount = Convert.ToInt32(countResult);
            }

            // 2. Thực thi truy vấn phân trang lấy danh sách dòng
            using (var dataCmd = conn.CreateCommand())
            {
                dataCmd.CommandText = rawSql;
                dataCmd.CommandType = CommandType.Text;

                foreach (var (k, v) in parameters)
                {
                    var p = dataCmd.CreateParameter();
                    p.ParameterName = k;
                    p.Value = v ?? DBNull.Value;
                    dataCmd.Parameters.Add(p);
                }

                // SequentialAccess tối ưu hóa bộ nhớ cho Big Data
                using var reader = await dataCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                var fieldCount = reader.FieldCount;
                var fieldNames = new string[fieldCount];
                for (int i = 0; i < fieldCount; i++)
                {
                    fieldNames[i] = reader.GetName(i);
                }

                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>(fieldCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var val = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                        row[fieldNames[i]] = val;
                    }
                    rows.Add(row);
                }
            }
        }
        finally
        {
            sw.Stop();
        }

        _logger.LogInformation("ExecuteDynamicQuery hoàn tất trong {ElapsedMs}ms, nạp {RowCount}/{TotalCount} dòng",
            sw.ElapsedMilliseconds, rows.Count, totalCount);

        return new DynamicReportResultDto(
            Columns: columns,
            Rows: rows,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            ExecutionTimeMs: sw.ElapsedMilliseconds
        );
    }

    public async Task<IEnumerable<Dictionary<string, object?>>> StreamRowsAsync(
        string rawSql,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conn = _dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        var rows = new List<Dictionary<string, object?>>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = rawSql;
        cmd.CommandType = CommandType.Text;

        foreach (var (k, v) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = k;
            p.Value = v ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        var fieldCount = reader.FieldCount;
        var fieldNames = new string[fieldCount];
        for (int i = 0; i < fieldCount; i++)
        {
            fieldNames[i] = reader.GetName(i);
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(fieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fieldCount; i++)
            {
                var val = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                row[fieldNames[i]] = val;
            }
            rows.Add(row);
        }

        return rows;
    }
}

