---
name: bpmn-frontend-adminlte-formio
description: Hướng dẫn phát triển giao diện doanh nghiệp AdminLTE 3 / SPA, Form.io Dynamic Forms kèm Cascading Loading, nhúng Elsa Studio Designer và Dashboard BI.
---

# Skill: Frontend AdminLTE 3, Form.io & Visual Designer

## 1. Nguyên tắc Trọng tâm
1. **Form.io Cascading**: Sử dụng `dataSrc: 'url'` với URL template chứa biến trường cha `{{ data.parentId }}`. Luôn thiết lập `clearOnRefresh: true` để dọn dẹp trường con khi trường cha đổi giá trị.
2. **Elsa Studio Embedding**: Nhúng Designer bằng IFrame toàn màn hình hoặc Web Components (`<elsa-workflow-definition-editor>`). Cấu hình CORS và Auth Token header.
3. **Dashboard Realtime**: Sử dụng ApexCharts hoặc Chart.js nạp dữ liệu từ `/api/analytics/metrics`. Kết hợp SignalR cập nhật số lượng task cần duyệt và SLA Warning badge.
