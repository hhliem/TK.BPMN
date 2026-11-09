using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Analytics.Application.Services;

/// <summary>
/// Động cơ tính toán thời hạn SLA theo giờ làm việc hành chính thực tế (TSK-702)
/// Loại trừ chính xác cuối tuần, giờ nghỉ trưa, ngày lễ theo lịch chuẩn Việt Nam.
/// </summary>
public class BusinessHoursSlaEngine : ISlaCalculationEngine
{
    private readonly WorkingCalendar _defaultCalendar;
    private readonly ILogger<BusinessHoursSlaEngine> _logger;

    public BusinessHoursSlaEngine(ILogger<BusinessHoursSlaEngine> logger)
    {
        _defaultCalendar = WorkingCalendar.CreateDefaultVietnamCalendar();
        _logger = logger;
    }

    public DateTime CalculateDueDate(DateTime submittedAt, double slaHours, WorkingCalendar? calendar = null)
    {
        var cal = calendar ?? _defaultCalendar;
        if (slaHours <= 0) return submittedAt;

        var totalMinutesNeeded = (int)Math.Ceiling(slaHours * 60);

        // 1. Chuẩn hóa thời gian bắt đầu vào khung giờ làm việc hợp lệ
        var current = NormalizeToWorkingTime(submittedAt, cal);

        // 2. Từng bước tiêu thụ số phút SLA qua các khung giờ làm việc
        while (totalMinutesNeeded > 0)
        {
            var dateOnly = DateOnly.FromDateTime(current);

            // Nếu rơi vào ngày không làm việc hoặc ngày lễ, chuyển sang sáng ngày làm việc kế tiếp
            if (!cal.IsWorkingDay(current.DayOfWeek) || cal.IsHoliday(dateOnly))
            {
                current = MoveToNextWorkingDayStart(current, cal);
                continue;
            }

            // Khung giờ sáng: 08:00 -> 12:00
            var morningStart = new DateTime(current.Year, current.Month, current.Day, cal.MorningStartHour, cal.MorningStartMinute, 0, current.Kind);
            var morningEnd = new DateTime(current.Year, current.Month, current.Day, cal.MorningEndHour, cal.MorningEndMinute, 0, current.Kind);

            // Khung giờ chiều: 13:30 -> 17:30
            var afternoonStart = new DateTime(current.Year, current.Month, current.Day, cal.AfternoonStartHour, cal.AfternoonStartMinute, 0, current.Kind);
            var afternoonEnd = new DateTime(current.Year, current.Month, current.Day, cal.AfternoonEndHour, cal.AfternoonEndMinute, 0, current.Kind);

            // Nếu đang trước giờ làm buổi sáng
            if (current < morningStart)
            {
                current = morningStart;
            }

            // Nếu đang trong ca sáng
            if (current >= morningStart && current < morningEnd)
            {
                var availableMinutes = (int)(morningEnd - current).TotalMinutes;
                if (totalMinutesNeeded <= availableMinutes)
                {
                    return current.AddMinutes(totalMinutesNeeded);
                }

                totalMinutesNeeded -= availableMinutes;
                current = afternoonStart; // Chuyển qua đầu giờ chiều (tự động bỏ qua 1h30p nghỉ trưa)
                continue;
            }

            // Nếu đang trong giờ nghỉ trưa
            if (current >= morningEnd && current < afternoonStart)
            {
                current = afternoonStart;
            }

            // Nếu đang trong ca chiều
            if (current >= afternoonStart && current < afternoonEnd)
            {
                var availableMinutes = (int)(afternoonEnd - current).TotalMinutes;
                if (totalMinutesNeeded <= availableMinutes)
                {
                    return current.AddMinutes(totalMinutesNeeded);
                }

                totalMinutesNeeded -= availableMinutes;
                // Hết ngày làm việc hôm nay, chuyển sang sáng ngày kế tiếp
                current = MoveToNextWorkingDayStart(current, cal);
                continue;
            }

            // Nếu đã quá giờ làm ca chiều, chuyển sang sáng hôm sau
            if (current >= afternoonEnd)
            {
                current = MoveToNextWorkingDayStart(current, cal);
            }
        }

        return current;
    }

    public double CalculateBusinessHoursElapsed(DateTime start, DateTime end, WorkingCalendar? calendar = null)
    {
        var cal = calendar ?? _defaultCalendar;
        if (start >= end) return 0.0;

        var current = NormalizeToWorkingTime(start, cal);
        long totalWorkingMinutes = 0;

        while (current < end)
        {
            var dateOnly = DateOnly.FromDateTime(current);

            // Bỏ qua ngày nghỉ cuối tuần hoặc ngày lễ
            if (!cal.IsWorkingDay(current.DayOfWeek) || cal.IsHoliday(dateOnly))
            {
                current = MoveToNextWorkingDayStart(current, cal);
                continue;
            }

            var morningStart = new DateTime(current.Year, current.Month, current.Day, cal.MorningStartHour, cal.MorningStartMinute, 0, current.Kind);
            var morningEnd = new DateTime(current.Year, current.Month, current.Day, cal.MorningEndHour, cal.MorningEndMinute, 0, current.Kind);
            var afternoonStart = new DateTime(current.Year, current.Month, current.Day, cal.AfternoonStartHour, cal.AfternoonStartMinute, 0, current.Kind);
            var afternoonEnd = new DateTime(current.Year, current.Month, current.Day, cal.AfternoonEndHour, cal.AfternoonEndMinute, 0, current.Kind);

            // Buổi sáng
            if (current < morningEnd)
            {
                var segStart = current < morningStart ? morningStart : current;
                var segEnd = end < morningEnd ? end : morningEnd;

                if (segStart < segEnd)
                {
                    totalWorkingMinutes += (long)(segEnd - segStart).TotalMinutes;
                    current = segEnd;
                }
            }

            if (current >= end) break;

            // Nghỉ trưa -> Nhảy qua đầu giờ chiều
            if (current >= morningEnd && current < afternoonStart)
            {
                current = afternoonStart;
            }

            if (current >= end) break;

            // Buổi chiều
            if (current >= afternoonStart && current < afternoonEnd)
            {
                var segStart = current;
                var segEnd = end < afternoonEnd ? end : afternoonEnd;

                if (segStart < segEnd)
                {
                    totalWorkingMinutes += (long)(segEnd - segStart).TotalMinutes;
                    current = segEnd;
                }
            }

            if (current >= end) break;

            // Hết ngày làm việc, nhảy sang sáng ngày kế tiếp
            if (current >= afternoonEnd)
            {
                current = MoveToNextWorkingDayStart(current, cal);
            }
        }

        return Math.Round((double)totalWorkingMinutes / 60.0, 2);
    }

    public bool IsWorkingTime(DateTime dateTime, WorkingCalendar? calendar = null)
    {
        var cal = calendar ?? _defaultCalendar;
        var dateOnly = DateOnly.FromDateTime(dateTime);

        if (!cal.IsWorkingDay(dateTime.DayOfWeek) || cal.IsHoliday(dateOnly))
            return false;

        var morningStart = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, cal.MorningStartHour, cal.MorningStartMinute, 0, dateTime.Kind);
        var morningEnd = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, cal.MorningEndHour, cal.MorningEndMinute, 0, dateTime.Kind);
        var afternoonStart = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, cal.AfternoonStartHour, cal.AfternoonStartMinute, 0, dateTime.Kind);
        var afternoonEnd = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, cal.AfternoonEndHour, cal.AfternoonEndMinute, 0, dateTime.Kind);

        return (dateTime >= morningStart && dateTime < morningEnd) ||
               (dateTime >= afternoonStart && dateTime < afternoonEnd);
    }

    public DateTime GetNextWorkingTime(DateTime dateTime, WorkingCalendar? calendar = null)
    {
        var cal = calendar ?? _defaultCalendar;
        return NormalizeToWorkingTime(dateTime, cal);
    }

    private static DateTime NormalizeToWorkingTime(DateTime dt, WorkingCalendar cal)
    {
        var current = dt;

        while (true)
        {
            var dateOnly = DateOnly.FromDateTime(current);

            if (!cal.IsWorkingDay(current.DayOfWeek) || cal.IsHoliday(dateOnly))
            {
                current = MoveToNextWorkingDayStart(current, cal);
                continue;
            }

            var morningStart = new DateTime(current.Year, current.Month, current.Day, cal.MorningStartHour, cal.MorningStartMinute, 0, current.Kind);
            var morningEnd = new DateTime(current.Year, current.Month, current.Day, cal.MorningEndHour, cal.MorningEndMinute, 0, current.Kind);
            var afternoonStart = new DateTime(current.Year, current.Month, current.Day, cal.AfternoonStartHour, cal.AfternoonStartMinute, 0, current.Kind);
            var afternoonEnd = new DateTime(current.Year, current.Month, current.Day, cal.AfternoonEndHour, cal.AfternoonEndMinute, 0, current.Kind);

            if (current < morningStart)
                return morningStart;

            if (current >= morningStart && current < morningEnd)
                return current;

            if (current >= morningEnd && current < afternoonStart)
                return afternoonStart;

            if (current >= afternoonStart && current < afternoonEnd)
                return current;

            // Nếu >= afternoonEnd thì nhảy sang đầu ngày làm việc kế tiếp
            current = MoveToNextWorkingDayStart(current, cal);
        }
    }

    private static DateTime MoveToNextWorkingDayStart(DateTime dt, WorkingCalendar cal)
    {
        var nextDay = dt.Date.AddDays(1);
        return new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, cal.MorningStartHour, cal.MorningStartMinute, 0, dt.Kind);
    }
}

