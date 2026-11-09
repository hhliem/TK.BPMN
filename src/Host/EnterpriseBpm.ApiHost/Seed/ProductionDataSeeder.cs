using EnterpriseBpm.Org.Domain.Entities;
using EnterpriseBpm.Org.Domain.Enums;
using EnterpriseBpm.Org.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.ApiHost.Seed;

/// <summary>
/// Trình nạp dữ liệu môi trường Vận hành Sản xuất (TSK-815):
/// Cây cấu trúc tổ chức tập đoàn đa cấp, danh mục chức danh chuẩn,
/// và 1,000 nhân sự doanh nghiệp mẫu kèm quan hệ quản lý trực tiếp.
/// </summary>
public static class ProductionDataSeeder
{
    public static async Task SeedEnterpriseDirectoryAsync(OrgDbContext orgDb, ILogger logger)
    {
        if (await orgDb.Employees.AnyAsync())
        {
            logger.LogInformation("[ProductionDataSeeder] Dữ liệu nhân sự doanh nghiệp đã tồn tại. Bỏ qua bước seed.");
            return;
        }

        logger.LogInformation("[ProductionDataSeeder] Đang bắt đầu khởi tạo 1,000 nhân sự và sơ đồ tổ chức doanh nghiệp...");

        // 1. Khởi tạo Sơ đồ Tổ chức Tập đoàn (OrgUnits)
        var rootGroup = new OrgUnit
        {
            Id = Guid.NewGuid(),
            Code = "TK-CORP",
            Name = "Tập đoàn TK Group",
            UnitType = UnitType.Group,
            DisplayOrder = 1,
            IsActive = true
        };
        orgDb.OrgUnits.Add(rootGroup);

        var bod = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "BOD",
            Name = "Ban Tổng Giám đốc",
            UnitType = UnitType.Division,
            DisplayOrder = 2,
            IsActive = true
        };
        var itDept = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "IT-DEPT",
            Name = "Ban Công nghệ Thông tin",
            UnitType = UnitType.Department,
            DisplayOrder = 3,
            IsActive = true
        };
        var financeDept = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "FIN-DEPT",
            Name = "Ban Tài chính & Kế toán",
            UnitType = UnitType.Department,
            DisplayOrder = 4,
            IsActive = true
        };
        var hrDept = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "HR-DEPT",
            Name = "Ban Nhân sự & Hành chính",
            UnitType = UnitType.Department,
            DisplayOrder = 5,
            IsActive = true
        };
        var salesDept = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "SALES-DEPT",
            Name = "Ban Kinh doanh & Tiếp thị",
            UnitType = UnitType.Department,
            DisplayOrder = 6,
            IsActive = true
        };
        var projectDept = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "PMO-DEPT",
            Name = "Ban Quản lý Dự án & Kỹ thuật",
            UnitType = UnitType.Department,
            DisplayOrder = 7,
            IsActive = true
        };
        var southBranch = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = rootGroup.Id,
            Code = "TK-SOUTH",
            Name = "Chi nhánh Miền Nam",
            UnitType = UnitType.Company,
            DisplayOrder = 8,
            IsActive = true
        };

        var deptList = new[] { bod, itDept, financeDept, hrDept, salesDept, projectDept, southBranch };
        orgDb.OrgUnits.AddRange(deptList);

        // 2. Khởi tạo Danh mục Chức danh (Positions)
        var posCeo = new Position { Id = Guid.NewGuid(), Code = "POS-CEO", Title = "Tổng Giám đốc", Level = 10, IsActive = true };
        var posDirector = new Position { Id = Guid.NewGuid(), Code = "POS-DIR", Title = "Giám đốc Khối / Giám đốc Chi nhánh", Level = 8, IsActive = true };
        var posManager = new Position { Id = Guid.NewGuid(), Code = "POS-MGR", Title = "Trưởng ban / Trưởng phòng", Level = 7, IsActive = true };
        var posDeputy = new Position { Id = Guid.NewGuid(), Code = "POS-DEP", Title = "Phó ban / Phó phòng", Level = 6, IsActive = true };
        var posLead = new Position { Id = Guid.NewGuid(), Code = "POS-LEAD", Title = "Trưởng nhóm Chuyên môn", Level = 5, IsActive = true };
        var posSrSpecialist = new Position { Id = Guid.NewGuid(), Code = "POS-SRS", Title = "Chuyên viên chính", Level = 4, IsActive = true };
        var posSpecialist = new Position { Id = Guid.NewGuid(), Code = "POS-SPE", Title = "Chuyên viên nghiệp vụ", Level = 3, IsActive = true };
        var posStaff = new Position { Id = Guid.NewGuid(), Code = "POS-STF", Title = "Nhân viên hỗ trợ", Level = 2, IsActive = true };

        orgDb.Positions.AddRange(posCeo, posDirector, posManager, posDeputy, posLead, posSrSpecialist, posSpecialist, posStaff);
        await orgDb.SaveChangesAsync();

        // 3. Khởi tạo 1,000 Nhân sự Doanh nghiệp Mẫu (Employees) với Cây Quản lý
        var familyNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương" };
        var middleNames = new[] { "Văn", "Thị", "Hải", "Quốc", "Đức", "Minh", "Thanh", "Đình", "Xuân", "Thành", "Hồng", "Tuấn", "Hoàng", "Gia" };
        var firstNames = new[] { "An", "Bình", "Cường", "Dũng", "Em", "Phương", "Giang", "Hương", "Huy", "Khoa", "Linh", "Long", "Nam", "Phúc", "Quân", "Sơn", "Tâm", "Thắng", "Trang", "Tùng", "Vinh" };

        var employees = new List<Employee>(1000);

        // 3.1. Ban Lãnh đạo cấp cao (CEO)
        var ceo = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "TK-00001",
            UserPrincipalName = "ceo@tkgroup.vn",
            FullName = "Nguyễn Văn Toàn",
            OrgUnitId = bod.Id,
            PositionId = posCeo.Id,
            JobTitle = posCeo.Title,
            WorkLocation = "Hội sở chính - Hà Nội",
            PhoneNumber = "0901000001",
            Extension = "101",
            IsActive = true,
            JoinDate = new DateOnly(2020, 1, 15)
        };
        employees.Add(ceo);

        // 3.2. Giám đốc các ban / chi nhánh (Direct Report to CEO)
        var managers = new List<Employee>();
        for (int d = 1; d < deptList.Length; d++)
        {
            var dept = deptList[d];
            var mgr = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = $"TK-{(d + 1):D5}",
                UserPrincipalName = $"director.{dept.Code.ToLower()}@tkgroup.vn",
                FullName = $"{familyNames[d % familyNames.Length]} {middleNames[d % middleNames.Length]} {firstNames[d % firstNames.Length]}",
                OrgUnitId = dept.Id,
                PositionId = posDirector.Id,
                JobTitle = $"Giám đốc {dept.Name}",
                DirectManagerId = ceo.Id,
                WorkLocation = dept.Code == "TK-SOUTH" ? "Chi nhánh TP. Hồ Chí Minh" : "Hội sở chính - Hà Nội",
                PhoneNumber = $"09010000{d + 1:D2}",
                Extension = $"20{d}",
                IsActive = true,
                JoinDate = new DateOnly(2021, 3, 1)
            };
            managers.Add(mgr);
            employees.Add(mgr);
        }

        // 3.3. Các Trưởng nhóm / Phó phòng (Direct Report to Managers)
        var teamLeads = new List<Employee>();
        for (int m = 0; m < managers.Count; m++)
        {
            var mgr = managers[m];
            for (int l = 1; l <= 3; l++)
            {
                var leadIdx = employees.Count + 1;
                var lead = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeCode = $"TK-{leadIdx:D5}",
                    UserPrincipalName = $"lead.{mgr.OrgUnitId.ToString()[..4]}.{l}@tkgroup.vn",
                    FullName = $"{familyNames[(leadIdx * 3) % familyNames.Length]} {middleNames[(leadIdx * 7) % middleNames.Length]} {firstNames[(leadIdx * 5) % firstNames.Length]}",
                    OrgUnitId = mgr.OrgUnitId,
                    PositionId = posLead.Id,
                    JobTitle = $"Trưởng nhóm {l} - {deptList.First(x => x.Id == mgr.OrgUnitId).Name}",
                    DirectManagerId = mgr.Id,
                    WorkLocation = mgr.WorkLocation,
                    PhoneNumber = $"0902{leadIdx:D6}",
                    Extension = $"3{l:D2}",
                    IsActive = true,
                    JoinDate = new DateOnly(2022, 5, 10)
                };
                teamLeads.Add(lead);
                employees.Add(lead);
            }
        }

        // 3.4. Điền đầy đủ 1,000 nhân viên (Chuyên viên & Nhân viên)
        var totalNeeded = 1000 - employees.Count;
        for (int i = 0; i < totalNeeded; i++)
        {
            var empIdx = employees.Count + 1;
            var assignedLead = teamLeads[i % teamLeads.Count];
            var isSenior = (i % 3) == 0;
            var pos = isSenior ? posSrSpecialist : (i % 2 == 0 ? posSpecialist : posStaff);

            var emp = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = $"TK-{empIdx:D5}",
                UserPrincipalName = $"nv{empIdx:D4}@tkgroup.vn",
                FullName = $"{familyNames[(i * 11) % familyNames.Length]} {middleNames[(i * 13) % middleNames.Length]} {firstNames[(i * 17) % firstNames.Length]}",
                OrgUnitId = assignedLead.OrgUnitId,
                PositionId = pos.Id,
                JobTitle = pos.Title,
                DirectManagerId = assignedLead.Id,
                WorkLocation = assignedLead.WorkLocation,
                PhoneNumber = $"0903{empIdx:D6}",
                Extension = $"{400 + (i % 500)}",
                IsActive = true,
                JoinDate = new DateOnly(2023, 1 + (i % 12), 1 + (i % 28))
            };
            employees.Add(emp);
        }

        // Lưu theo từng batch 250 records để tối ưu hoá bộ nhớ và tốc độ
        var batchSize = 250;
        for (int i = 0; i < employees.Count; i += batchSize)
        {
            var batch = employees.Skip(i).Take(batchSize).ToList();
            orgDb.Employees.AddRange(batch);
            await orgDb.SaveChangesAsync();
            logger.LogInformation($"[ProductionDataSeeder] Đã lưu thành công lô nhân sự {i + 1} - {Math.Min(i + batchSize, employees.Count)} / 1000.");
        }

        logger.LogInformation($"[ProductionDataSeeder] Hoàn thành nạp thành công 1,000 nhân viên, 8 phòng ban và 8 chức danh doanh nghiệp!");
    }
}

