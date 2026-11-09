-- =========================================================================
-- Trigger Bảo vệ Tính Bất biến của Nhật ký Kiểm toán (Immutable Audit Log)
-- Cấm hoàn toàn mọi câu lệnh UPDATE hoặc DELETE trên bảng [audit].[AuditEntries]
-- =========================================================================
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_AuditEntries_PreventModification' AND schema_id = SCHEMA_ID('audit'))
BEGIN
    DROP TRIGGER [audit].[TR_AuditEntries_PreventModification];
END;
GO

CREATE TRIGGER [audit].[TR_AuditEntries_PreventModification]
ON [audit].[AuditEntries]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR('Audit entries cannot be modified or deleted. (Dữ liệu kiểm toán không thể bị sửa hoặc xóa!)', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

