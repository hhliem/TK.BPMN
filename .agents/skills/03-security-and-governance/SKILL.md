---
name: bpmn-security-governance
description: Tiêu chuẩn bảo mật RBAC/ABAC, chống giả mạo Cascading Data (FluentValidation), kiểm soát ủy quyền Delegation và ranh giới bảo mật AI MCP.
---

# Skill: Enterprise Security & Governance

## 1. Điểm Kiểm Soát Trọng Yếu
1. **Anti-Tampering Validation**: Tuyệt đối không tin tưởng payload Form.io gửi lên. Server-side validation bằng FluentValidation phải query xác thực dữ liệu con thực sự trực thuộc dữ liệu cha.
2. **Circular Delegation Prevention**: Chặn hoàn toàn trường hợp ủy quyền vòng tròn (User A ủy quyền B, B ủy quyền lại A trong cùng khung thời gian).
3. **Legal Audit Trail**: Khi người được ủy quyền (Delegatee) duyệt đơn, log hệ thống bắt buộc ghi nhận: `Ký thay [Delegator] theo ủy quyền số #...`.
4. **AI MCP Data Boundary**: Mọi truy vấn từ Local MCP Tools phải tiêm `CurrentUserId` và `OrgUnitId` của người đang chat, ngăn AI đọc trộm dữ liệu ngoài quyền hạn.
