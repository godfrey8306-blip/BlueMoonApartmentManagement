# BlueMoon Apartment Management

## 1. Giới thiệu dự án

**BlueMoon Apartment Management** là phần mềm hỗ trợ quản lý hoạt động chung cư, tập trung vào quản lý cư dân, hộ gia đình và các khoản thu phí.

Dự án được thực hiện trong khuôn khổ học phần Quản lý dự án phần mềm.

## 2. Mục tiêu

- Quản lý thông tin cư dân và hộ gia đình.
- Quản lý danh mục các khoản phí chung cư.
- Hỗ trợ ghi nhận và theo dõi thanh toán.
- Tra cứu và thống kê tình hình thu phí.
- Hỗ trợ quản lý dữ liệu tập trung.

## 3. Phạm vi chức năng

- Quản lý tài khoản và phân quyền.
- Quản lý cư dân.
- Quản lý hộ gia đình.
- Quản lý các khoản phí.
- Quản lý thanh toán.
- Thống kê và báo cáo.

## 4. Công nghệ sử dụng

| Thành phần | Công nghệ | Mục đích |
|---|---|---|
| Backend | ASP.NET Core Web API (C#) | Xây dựng API và xử lý nghiệp vụ hệ thống |
| Frontend | React (JavaScript) | Xây dựng giao diện người dùng |
| Database | PostgreSQL | Lưu trữ và quản lý dữ liệu |
| ORM | Entity Framework Core | Kết nối và thao tác dữ liệu giữa backend và database |
| UI Framework | Bootstrap | Thiết kế giao diện responsive |
| API Testing | Postman | Kiểm thử các API |
| Version Control | Git | Quản lý phiên bản mã nguồn |
| Repository | GitHub | Lưu trữ mã nguồn và phối hợp phát triển nhóm |

### Kiến trúc hệ thống

Hệ thống được xây dựng theo mô hình Client – Server:

- **Frontend (React):** Tiếp nhận thao tác từ người dùng và hiển thị dữ liệu.
- **Backend (ASP.NET Core Web API):** Tiếp nhận yêu cầu từ frontend, xử lý nghiệp vụ và trả về dữ liệu thông qua RESTful API.
- **Database (PostgreSQL):** Lưu trữ thông tin cư dân, căn hộ, khoản phí, thanh toán và các dữ liệu liên quan.
- **Entity Framework Core:** Hỗ trợ backend truy vấn và cập nhật dữ liệu trong database.

### Công cụ phát triển

- Visual Studio / Visual Studio Code
- PostgreSQL và DBeaver
- Postman
- Git và GitHub


## 5. Cấu trúc repository

BlueMoonApartmentManagement/
├── docs/
├── src/
├── tests/
├── reports/
├── README.md
└── .gitignore


## 6. Thành viên dự án

Nhóm: SE_01

20226114	Nguyễn Lâm Tường	Minh
20226119	Nguyễn Hải	Phong
202417247	LÊ XUÂN	LƯƠNG
202417258	NGUYỄN TUẤN	NGHĨA


## 7. Quản lý phiên bản

- `main`: Phiên bản ổn định.
- `develop`: Nhánh phát triển chung.
- `feature/*`: Nhánh phát triển chức năng riêng.

## 8. Trạng thái dự án

Đang triển khai Sprint 1.
