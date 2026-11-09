# Đặc tả Cấu hình Form.io Cascading Loading

## Nguyên lý
- Parent Field: Select chọn Khối / Phòng ban (key: `departmentId`).
- Child Field: Select chọn Chức vụ / Nhân sự thuộc phòng ban (key: `positionId`).

## Quy tắc JSON Schema:
- URL child: `/api/master-data/positions?departmentId={{ data.departmentId }}`
- Bắt buộc: `clearOnRefresh: true`
- Lazy-load: Chỉ kích hoạt request khi `data.departmentId` khác null/empty.
