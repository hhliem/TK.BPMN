# 🤖 Frontend Sprint 06: Local AI Copilot Chat Widget & Policy Q&A

---

## 1. Thông tin Chung về FE-Sprint 06

- **Tên Sprint:** Frontend Sprint 06 — Floating AI Copilot Widget, Markdown Streaming & Policy Citations
- **Thời gian Thực hiện:** Tuần 11 - Tuần 12 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng Widget Trợ lý Ảo Doanh nghiệp (Floating Copilot Widget) nổi ở góc màn hình có thể kích hoạt nhanh bằng phím tắt (`Ctrl + Space`), giao diện trò chuyện hỗ trợ hiệu ứng hiển thị từng chữ (Server-Sent Events / Markdown Streaming), Thẻ Trích dẫn Quy chế Doanh nghiệp (Citation Cards) liên kết mở Drawer xem nguyên văn điều khoản nội bộ từ Qdrant Vector DB, và bộ chuyển đổi linh hoạt mô hình AI (Gemini, ChatGPT, Claude, Local MCP).
- **Tổng Story Points cam kết:** **45 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **225 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-29 & FE-US-30: Widget AI Nổi & Streaming Trò chuyện (21 SP)
- **User Story:** Là nhân viên đang làm việc trên hệ thống, tôi muốn mở nhanh trợ lý AI bằng phím tắt hoặc nút bấm nổi để hỏi đáp nghiệp vụ, câu trả lời hiển thị gõ từng chữ mượt mà.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Mở nhanh Widget AI bằng phím tắt
    Given người dùng đang ở bất kỳ màn hình nào trong ứng dụng
    When người dùng nhấn tổ hợp phím "Ctrl + Space" (hoặc "Cmd + K" trên macOS)
    Then cửa sổ Chat Widget AI mở ra ngay lập tức với tiêu điểm (focus) nằm tại ô nhập câu hỏi
    And khi nhấn phím "Escape", cửa sổ chat thu gọn trở lại về biểu tượng nút tròn nổi góc phải dưới.

  Scenario: Trò chuyện và nhận câu trả lời dạng Streaming Markdown
    Given người dùng gõ câu hỏi: "Hạn mức thanh toán tiếp khách của Trưởng phòng là bao nhiêu?" và bấm gửi
    When Backend bắt đầu stream dữ liệu qua Server-Sent Events (SSE)
    Then câu trả lời hiển thị hiệu ứng gõ chữ thời gian thực (typing effect), không bị đơ trình duyệt
    And nội dung Markdown được tự động định dạng chuẩn: chữ in đậm, danh sách dấu chấm, và bảng dữ liệu nếu có
    And có nút "Dừng sinh câu trả lời" (Stop generating) để ngắt luồng nếu người dùng muốn.
  ```

### FE-US-31: Thẻ Trích dẫn Quy chế (Citation Cards) & Drawer Xem Chi tiết (11 SP)
- **User Story:** Là người kiểm tra quy định, tôi muốn biết AI lấy thông tin từ văn bản quy chế nào, có thể bấm vào thẻ trích dẫn để đọc trực tiếp nguyên văn điều khoản liên quan.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Bấm vào thẻ trích dẫn quy chế mở Drawer đối chiếu
    Given câu trả lời của AI có trích dẫn từ tài liệu quy chế
    When dưới câu trả lời xuất hiện các Thẻ trích dẫn: "[QĐ-452/2025/TTr - Điều 8, Khoản 2 - Độ khớp: 94%]"
    And người dùng nhấp chuột vào Thẻ trích dẫn đó
    Then một Drawer trượt ra từ bên phải màn hình hiển thị nguyên văn đoạn văn bản trích xuất từ Qdrant
    And từ khóa liên quan được tô màu highlight màu vàng để đối chiếu tức thì.
  ```

### FE-US-32 & FE-US-33: Bộ Chọn Nhà Cung Cấp AI & Đánh Giá Hài Lòng (13 SP)
- **User Story:** Là người dùng, tôi muốn có thể chuyển đổi linh hoạt giữa các mô hình AI khác nhau và gửi đánh giá (Thumbs up/down) để cải thiện chất lượng câu trả lời.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Chuyển đổi Nhà cung cấp AI
    Given người dùng đang ở cửa sổ Chat AI
    When nhấp vào Dropdown "Mô hình AI" và chọn "Google Gemini 2.0 Flash" (hoặc "OpenAI GPT-4o", "Claude 3.5 Sonnet")
    Then câu hỏi tiếp theo sẽ được gửi đến đúng Provider đã chọn
    And biểu tượng nhà cung cấp tương ứng hiển thị trên tiêu đề cửa sổ trò chuyện.

  Scenario: Đánh giá câu trả lời của AI
    Given AI vừa hoàn thành sinh câu trả lời
    When người dùng bấm vào biểu tượng "Thumbs Down" (Không hài lòng)
    Then một Popover nhỏ bật lên hỏi lý do: "Thông tin không chính xác", "Quy chế đã hết hiệu lực", "Câu trả lời khó hiểu"
    And sau khi chọn lý do, hệ thống gửi phản hồi về API POST /api/ai/feedback để tinh chỉnh hệ thống RAG.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-601** | AI Widget Shell | Floating Launcher Button | Dựng `FloatingAiLauncher.vue` nút tròn nổi góc phải dưới màn hình với icon robot AI, badge trạng thái và hiệu ứng ripple. | 12 | Frontend Dev 1 |
| **TSK-FE-602** | AI Widget Shell | Global Hotkey Listener | Viết composable `useGlobalHotkey.ts` bắt sự kiện `Ctrl + Space` / `Cmd + K` và phím `Escape` để toggle widget. | 8 | Frontend Dev 2 |
| **TSK-FE-603** | AI Widget Shell | Chat Window Panel | Dựng `AiChatWindow.vue` hỗ trợ 3 chế độ: Cửa sổ nổi nhỏ (420px), Cột bên phải (Side-panel 550px) và Toàn màn hình. | 18 | Frontend Dev 1 |
| **TSK-FE-604** | Chat Engine | SSE / Fetch Stream Client | Viết composable `useAiStream.ts` sử dụng `fetch` + `ReadableStream` đọc dữ liệu text stream từ API `/api/ai/chat/stream`. | 20 | Tech Lead |
| **TSK-FE-605** | Chat Engine | Markdown & Code Highlighter | Tích hợp thư viện render Markdown (`markdown-it` hoặc `marked`) kèm highlight cú pháp bảng biểu, danh sách và công thức. | 16 | Frontend Dev 2 |
| **TSK-FE-606** | Chat Input | Auto-expanding Input Bar | Dựng `ChatInputBar.vue` tự động giãn chiều cao theo nội dung gõ, phím tắt Shift+Enter xuống dòng, nút Send và Stop stream. | 14 | Frontend Dev 3 |
| **TSK-FE-607** | RAG Citations | Policy Citation Badge Card | Dựng `PolicyCitationBadge.vue` hiển thị số hiệu văn bản, trích lục điều khoản, điểm tương đồng (Similarity score %) và nút xem. | 14 | Frontend Dev 3 |
| **TSK-FE-608** | RAG Citations | Policy Deep-dive Drawer | Dựng `PolicyDocumentDrawer.vue` hiển thị toàn văn tài liệu quy chế, bôi vàng highlight từ khóa tìm kiếm và thông tin ngày ban hành. | 18 | Frontend Dev 1 |
| **TSK-FE-609** | AI Controls | Multi-Provider Switcher | Dựng dropdown chọn model (Google Gemini, OpenAI GPT-4o, Claude 3.5 Sonnet, Local MCP) và hiển thị token usage ước tính. | 12 | Frontend Dev 2 |
| **TSK-FE-610** | AI Controls | Suggested Starter Prompts | Dựng danh sách thẻ câu hỏi gợi ý thường gặp (Quick Prompts) xuất hiện khi bắt đầu phiên trò chuyện mới. | 10 | Frontend Dev 3 |
| **TSK-FE-611** | Feedback & History | Feedback Modal & Session History | Dựng cơ chế lưu phiên chat tạm thời, nút "Cuộc trò chuyện mới" và modal đánh giá phản hồi Thumbs Up/Down. | 16 | Frontend Dev 2 |
| **TSK-FE-612** | QA Testing | Unit & E2E Tests AI Copilot | Viết Unit Tests kiểm tra SSE stream decoder, hotkey listener và E2E test luồng hỏi AI -> xem citation drawer -> đánh giá feedback. | 22 | QA Engineer |
| **TỔNG** | | | **Tổng số 12 Tasks Kỹ thuật** | **180h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 06

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Đang ở màn hình bất kỳ, bấm tổ hợp phím `Ctrl + Space` $\rightarrow$ Cửa sổ AI Copilot bật lên ngay tức khắc, con trỏ chuột nhảy sẵn vào ô nhập câu hỏi.
- [ ] **Demo 2:** Gửi câu hỏi "Quy định về thanh toán chi phí tiếp khách" $\rightarrow$ Câu trả lời xuất hiện gõ từng chữ mượt mà, văn bản hiển thị đẹp mắt với chữ in đậm và bảng phân cấp hạn mức.
- [ ] **Demo 3:** Cuối câu trả lời có Thẻ trích dẫn "[Quy chế Tài chính số 12/2025/QĐ - Điều 15]" $\rightarrow$ Nhấp chuột vào thẻ, Drawer trượt ra hiển thị đúng nguyên văn bản quy chế với đoạn văn bản được bôi vàng nổi bật.
- [ ] **Demo 4:** Nhấp chọn chuyển Provider từ Gemini sang OpenAI GPT-4o $\rightarrow$ Giao diện đổi icon nhà cung cấp và truy vấn được thực hiện chuẩn xác.
- [ ] **Demo 5:** Bấm Thumbs Down $\rightarrow$ Popover hiện lên để chọn lý do phản hồi; gửi thành công và ghi nhận vào hệ thống quản trị dữ liệu.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 06
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **Giật lag giao diện khi stream text quá nhanh:** Render Markdown lại toàn bộ DOM sau mỗi ký tự nhận được | Cao | Sử dụng kỹ thuật RequestAnimationFrame (RAF) hoặc throttle cập nhật DOM 50ms/lần thay vì cập nhật sau từng token ký tự. |
| **XSS Vulnerability từ nội dung Markdown:** Kẻ xấu chèn mã script độc hại vào văn bản quy chế | Cao | Bắt buộc chạy qua thư viện lọc mã độc `DOMPurify` trước khi đưa nội dung Markdown vào `v-html`. |
