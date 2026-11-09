# 🎨 Enterprise BPM Platform — Frontend Single Page Application (Vue.js 3)

Nền tảng Giao diện Người dùng Hiện đại (Single Page Application - SPA) cho **Enterprise BPMN Platform**, được xây dựng trên **Vue.js 3.5+ (Composition API & TypeScript)**, **Vite 6**, **Pinia**, **Tailwind CSS v3.4+** và tích hợp bộ tri thức thiết kế chuyên sâu **Skill 02 (`02-frontend-ui-ux-pro-max`)**.

---

## 1. Cấu trúc Thư mục Khoa học & Chuẩn Doanh nghiệp

Hệ thống thư mục được tổ chức theo mô hình **Domain-driven Feature Modules** tương thích trực tiếp với 11 Bounded Contexts của Backend:

```
frontend/
├── public/                     # Tài nguyên tĩnh (Favicon, Logo SVG, Fonts)
├── src/
│   ├── assets/                 # CSS Tokens, Keyframe Animations, Images
│   │   └── styles/
│   │       ├── tokens.css      # CSS Semantic Tokens (Skill 02 Master Design System)
│   │       ├── animations.css  # Vi tương tác, Shimmer, Transitions
│   │       └── main.css        # Điểm nhập CSS chính kết hợp Tailwind
│   ├── components/             # Các UI Components dùng chung (Atoms & Molecules)
│   │   ├── ui/                 # Headless Primitives: Button, Input, Badge, Avatar, Tooltip
│   │   ├── feedback/           # Toasts, Confirm Modals, Loading Overlay
│   │   └── navigation/         # Breadcrumbs, User Profile Dropdown
│   ├── layouts/                # Các bộ khung App Shell
│   │   ├── AppLayout.vue       # Master Layout (Sidebar + Header + Main Area)
│   │   ├── AppSidebar.vue      # Sidebar thu gọn/mở rộng (68px / 260px) kèm tooltip
│   │   └── AppHeader.vue       # Header cố định (Breadcrumb, Search, Theme toggle)
│   ├── modules/                # Feature Modules theo từng phân hệ nghiệp vụ
│   │   ├── auth/               # Phân hệ Xác thực & Quản trị tài khoản
│   │   │   ├── api/            # API Service riêng của module
│   │   │   ├── components/     # Components nội bộ của module
│   │   │   ├── stores/         # Pinia store riêng (useAuthStore)
│   │   │   ├── types/          # TypeScript interfaces/types của module
│   │   │   └── views/          # Màn hình (LoginView.vue, ...)
│   │   └── common/             # Các màn hình dùng chung (Dashboard, 403, 404, 500)
│   ├── router/                 # Vue Router 4 & RBAC Navigation Guards
│   │   ├── index.ts            # Khởi tạo Router & Middleware kiểm tra quyền
│   │   └── routes.ts           # Khai báo danh mục Route và quyền tương ứng
│   ├── stores/                 # Các Pinia Stores toàn cục (Theme, Navigation, Global Loading)
│   ├── services/               # Tầng dịch vụ hạ tầng mạng & lưu trữ
│   │   ├── api/
│   │   │   └── apiClient.ts    # Axios Instance (Bearer Token, Correlation-ID, Refresh Queue)
│   │   └── storage/
│   │       └── tokenStorage.ts # Quản lý Access/Refresh token an toàn
│   ├── directives/             # Custom Directives (v-permission)
│   ├── types/                  # Global Type Definitions (Result<T>, NavigationItem)
│   ├── utils/                  # Helper Functions (Permissions, Formatters)
│   ├── App.vue                 # Root Component
│   └── main.ts                 # Application Entry Point
├── tests/                      # Thư mục kiểm thử tự động (Unit & E2E)
├── index.html                  # HTML Shell với kịch bản chống nhấp nháy FOUC
├── package.json
├── tailwind.config.ts          # Cấu hình Tailwind CSS theo Design Tokens
├── tsconfig.json               # Cấu hình TypeScript Strict Mode
└── vite.config.ts              # Cấu hình Vite 6 & Reverse Proxy tới Backend
```

---

## 2. Quy chuẩn Đặt tên File & Thư mục (Naming Conventions)

| Đối tượng | Quy chuẩn Đặt tên | Ví dụ Cụ thể |
|---|---|---|
| **Vue Components** | `PascalCase.vue` | `AppButton.vue`, `UserProfileModal.vue`, `LoginView.vue` |
| **TypeScript Modules / API** | `camelCase.ts` | `authApi.ts`, `apiClient.ts`, `tokenStorage.ts` |
| **Pinia Stores** | `use[Name]Store.ts` | `useAuthStore.ts`, `useNavigationStore.ts`, `useAppThemeStore.ts` |
| **Custom Directives** | `v[Name].ts` | `vPermission.ts` |
| **Type Definitions** | `[domain].types.ts` | `auth.types.ts`, `api.types.ts`, `navigation.types.ts` |
| **Thư mục (Directories)** | `kebab-case` | `components/ui`, `services/api`, `modules/auth` |

---

## 3. Bảng Màu Ngữ nghĩa & Design Tokens (Skill 02 UI/UX Pro Max)

- **Primary (`#1E3A5F`):** Xanh Hải quân tin cậy cho khối ngân hàng / tài chính doanh nghiệp.
- **Secondary (`#2563EB`):** Xanh hoàng gia cho các điểm nhấn phụ và trạng thái liên kết.
- **Accent / CTA (`#16A34A`):** Xanh lá Emerald cho nút Phê duyệt, Ký số và hành động thành công.
- **Destructive (`#DC2626`):** Đỏ thắm cho nút Từ chối, Yêu cầu làm rõ và Cảnh báo trễ hạn SLA.
- **Typography:**
  - Tiêu đề: **Lexend** (Google Fonts)
  - Thân bài: **Source Sans 3** (Google Fonts)
- **Biểu tượng (Iconography):** Sử dụng **100% SVG Vector (Lucide Icons)**, tuyệt đối không dùng Emoji làm biểu tượng hệ thống.

---

## 4. Hướng dẫn Chạy Ứng dụng

```bash
# Di chuyển vào thư mục frontend
cd frontend

# Cài đặt thư viện phụ thuộc
npm install

# Khởi chạy máy chủ phát triển (Dev Server) tại cổng 3000
npm run dev

# Kiểm tra an toàn kiểu dữ liệu TypeScript (Type check)
npm run type-check

# Đóng gói sản phẩm cho môi trường Production
npm run build
```
