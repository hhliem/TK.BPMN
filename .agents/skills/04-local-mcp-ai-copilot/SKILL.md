---
name: bpmn-local-mcp-ai-copilot
description: Thiết kế Local MCP Server, triển khai C# MCP Tools phân tích dữ liệu trình ký an toàn và tích hợp RAG Qdrant Vector DB tra cứu quy chế nội bộ.
---

# Skill: Multi-Provider AI Assistant, Local MCP Server & Enterprise RAG

## 1. Nguyên tắc Trọng tâm
1. **Multi-Provider & Model Agnostic**: Không phụ thuộc vào một mô hình đơn lẻ. Hỗ trợ cắm-và-chạy linh hoạt giữa Google Gemini, OpenAI ChatGPT, Anthropic Claude và Local LLM (Ollama/vLLM) qua tầng trừu tượng `IAiChatClient`.
2. **Unified C# Tool Definitions**: Mọi C# Tool được định nghĩa một lần với metadata chuẩn hóa, tự động ánh xạ sang Function Calling của Gemini, OpenAI, Claude và Local MCP Server.
3. **Parameterized Queries Only**: Tuyệt đối không cho phép LLM sinh câu lệnh SQL raw để chạy trực tiếp vào SQL Server. LLM chỉ được gọi các C# Tools định nghĩa sẵn.
4. **Context Enforced Isolation**: Tool tự động tiêm ngữ cảnh người dùng đang đăng nhập (`CurrentUserId`, `Role`, `OrgUnitId`) vào câu truy vấn để bảo đảm ranh giới phân quyền RBAC/ABAC.
5. **Qdrant Vector DB for Policies**: Tài liệu biểu mẫu, quy trình, quy chế nội bộ được chunking và embedding lưu vào Qdrant để AI tra cứu thông tin chính xác kèm trích dẫn nguồn (Citations).
