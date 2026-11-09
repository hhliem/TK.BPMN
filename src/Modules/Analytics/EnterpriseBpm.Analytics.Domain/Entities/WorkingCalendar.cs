using System.Text.Json;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Analytics.Domain.Entities;

/// <summary>
/// Thực thể lịch làm việc hành chính doanh nghiệp (Working Calendar - TSK-701)
/// Quản lý khung giờ hành chính, giờ nghỉ trưa, ngày cuối tuần và các ngày lễ chính thức.
/// </summary>
public class WorkingCalendar : BaseEntity<Guid>, IAuditable
{
    public string CalendarCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int TimezoneOffsetHours { get; private set; } = 7; // Mặc định GMT+7 (Việt Nam)

    // Khung giờ buổi sáng
    public int MorningStartHour { get; private set; } = 8;
    public int MorningStartMinute { get; private set; } = 0;
    public int MorningEndHour { get; private set; } = 12;
    public int MorningEndMinute { get; private set; } = 0;

    // Khung giờ nghỉ trưa
    public int LunchStartHour { get; private set; } = 12;
    public int LunchStartMinute { get; private set; } = 0;
    public int LunchEndHour { get; private set; } = 13;
    public int LunchEndMinute { get; private set; } = 30;

    // Khung giờ buổi chiều
    public int AfternoonStartHour { get; private set; } = 13;
    public int AfternoonStartMinute { get; private set; } = 30;
    public int AfternoonEndHour { get; private set; } = 17;
    public int AfternoonEndMinute { get; private set; } = 30;

    // Ngày làm việc trong tuần (Mặc định: Thứ 2 -> Thứ 6)
    public bool IsMondayWorking { get; private set; } = true;
    public bool IsTuesdayWorking { get; private set; } = true;
    public bool IsWednesdayWorking { get; private set; } = true;
    public bool IsThursdayWorking { get; private set; } = true;
    public bool IsFridayWorking { get; private set; } = true;
    public bool IsSaturdayWorking { get; private set; } = false;
    public bool IsSundayWorking { get; private set; } = false;

    // Danh sách ngày nghỉ lễ (JSON mảng chuỗi format YYYY-MM-DD)
    public string HolidaysJson { get; private set; } = "[]";

    public bool IsDefault { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected WorkingCalendar() { }

    public static WorkingCalendar CreateDefaultVietnamCalendar()
    {
        return new WorkingCalendar
        {
            Id = Guid.NewGuid(),
            CalendarCode = "CAL-VN-STANDARD",
            Name = "Lịch Làm Việc Tiêu Chuẩn Hành Chính Việt Nam (Thứ 2 - Thứ 6: 08:00 - 17:30)",
            TimezoneOffsetHours = 7,
            MorningStartHour = 8,
            MorningStartMinute = 0,
            MorningEndHour = 12,
            MorningEndMinute = 0,
            LunchStartHour = 12,
            LunchStartMinute = 0,
            LunchEndHour = 13,
            LunchEndMinute = 30,
            AfternoonStartHour = 13,
            AfternoonStartMinute = 30,
            AfternoonEndHour = 17,
            AfternoonEndMinute = 30,
            IsMondayWorking = true,
            IsTuesdayWorking = true,
            IsWednesdayWorking = true,
            IsThursdayWorking = true,
            IsFridayWorking = true,
            IsSaturdayWorking = false,
            IsSundayWorking = false,
            // Nghỉ lễ cơ bản năm 2026/2027 (Tết Dương lịch, Giỗ tổ Hùng Vương, 30/4 - 1/5, Quốc khánh)
            HolidaysJson = JsonSerializer.Serialize(new[]
            {
                "2026-01-01", "2026-02-16", "2026-02-17", "2026-02-18", "2026-02-19", "2026-02-20",
                "2026-04-26", "2026-04-30", "2026-05-01", "2026-09-02", "2026-09-03",
                "2027-01-01"
            }),
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsWorkingDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => IsMondayWorking,
            DayOfWeek.Tuesday => IsTuesdayWorking,
            DayOfWeek.Wednesday => IsWednesdayWorking,
            DayOfWeek.Thursday => IsThursdayWorking,
            DayOfWeek.Friday => IsFridayWorking,
            DayOfWeek.Saturday => IsSaturdayWorking,
            DayOfWeek.Sunday => IsSundayWorking,
            _ => false
        };
    }

    public bool IsHoliday(DateOnly date)
    {
        var str = date.ToString("yyyy-MM-dd");
        try
        {
            var holidays = JsonSerializer.Deserialize<List<string>>(HolidaysJson);
            return holidays != null && holidays.Contains(str);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tổng số phút làm việc trong một ngày hành chính chuẩn (8 giờ = 480 phút)
    /// Buổi sáng: 08:00 - 12:00 (4h = 240p)
    /// Buổi chiều: 13:30 - 17:30 (4h = 240p)
    /// </summary>
    public int GetDailyWorkingMinutes()
    {
        var morningMinutes = (MorningEndHour * 60 + MorningEndMinute) - (MorningStartHour * 60 + MorningStartMinute);
        var afternoonMinutes = (AfternoonEndHour * 60 + AfternoonEndMinute) - (AfternoonStartHour * 60 + AfternoonStartMinute);
        return Math.Max(0, morningMinutes) + Math.Max(0, afternoonMinutes);
    }
}

