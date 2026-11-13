# HLT01 – Hệ thống đặt lịch khám và điều phối hàng đợi

Đồ án môn **Phương pháp phát triển phần mềm hướng đối tượng** – Trường Đại học Tài nguyên và Môi trường TP. Hồ Chí Minh, Khoa Công nghệ thông tin.

- Sinh viên: Phạm Gia Hiếu – MSSV 1250080053 – Lớp ĐH_K12_CNPM1
- Giảng viên hướng dẫn: ThS. Phạm Trọng Huynh

## Giới thiệu

Hệ thống giúp phòng khám đa khoa quản lý lịch khám và hàng đợi:

- **Bệnh nhân**: xem khung giờ trống, đặt lịch, hủy lịch, theo dõi số thứ tự và thông báo.
- **Nhân viên tiếp đón**: xác nhận hoặc từ chối lịch, gán mức ưu tiên lâm sàng, đổi lịch, check-in cấp số thứ tự.
- **Bác sĩ**: xem lịch khám, gọi bệnh nhân tiếp theo, hoàn tất hoặc bỏ qua lượt khám.
- **Quản trị viên**: quản lý tài khoản và vai trò, khoa, ca làm việc, mức ưu tiên.
- **Màn hình phòng chờ** (`/display`): hiển thị số đang gọi, tự cập nhật 5 giây một lần.

Hệ thống chống đặt trùng khung giờ khi nhiều người thao tác cùng lúc bằng chỉ mục duy nhất có điều kiện và khóa lạc quan (`rowversion`), và áp dụng các mẫu thiết kế State, Strategy, Observer, Repository, Unit of Work.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Máy chủ | C#, ASP.NET Core Web API (.NET 10), Entity Framework Core |
| Cơ sở dữ liệu | Microsoft SQL Server 2022 |
| Giao diện | React 19, Vite, React Router |
| Xác thực | Keycloak 26 (OpenID Connect, PKCE), giao diện đăng nhập tiếng Việt |
| Triển khai | Docker, Docker Compose |
| Kiểm thử | xUnit |

## Cấu trúc thư mục

```
HLT01-datlichkham/
├── README.md
├── IMAGES/                      # Sơ đồ UML, ERD, thiết kế giao diện, bảng biểu của báo cáo
└── PROJECT/                     # Toàn bộ mã nguồn
    ├── src/
    │   ├── Hlt.Domain/          # Lớp miền: thực thể, quy tắc nghiệp vụ, mẫu State, Strategy
    │   ├── Hlt.Application/     # Lớp ứng dụng: dịch vụ theo use case, DTO, Observer
    │   ├── Hlt.Infrastructure/  # Lớp hạ tầng: EF Core, Repository, Unit of Work, gửi thông báo
    │   └── Hlt.Api/             # Lớp API: controller, xác thực JWT, xử lý lỗi
    ├── tests/Hlt.Domain.Tests/  # Kiểm thử đơn vị lớp miền
    ├── web/                     # Ứng dụng React
    ├── keycloak/                # Realm hlt01 và giao diện đăng nhập tiếng Việt
    ├── tools/                   # Kịch bản kiểm thử đặt lịch đồng thời
    ├── docker-compose.yml       # Chạy toàn bộ hệ thống
    └── docker-compose.dev.yml   # Chỉ chạy SQL Server + Keycloak khi phát triển
```

## Cài đặt và chạy

Yêu cầu: Docker Desktop. Nếu chạy cách 2 cần thêm .NET SDK 10 và Node.js 20 trở lên.

### Bước chung: tạo tệp mật khẩu

Mọi lệnh bên dưới chạy trong thư mục `PROJECT`:

```bash
git clone https://github.com/GiaHieu29/HLT01-datlichkham.git
cd HLT01-datlichkham/PROJECT
```

Mật khẩu **không** được lưu trên GitHub. Sao chép `.env.example` thành `.env` rồi điền mật khẩu:

```bash
cp .env.example .env        # Windows PowerShell: Copy-Item .env.example .env
```

### Cách 1 – Chạy toàn bộ bằng Docker (một lệnh)

```bash
docker compose up -d --build
```

Mở http://localhost:3000. Lần đầu Keycloak và SQL Server cần khoảng 1–2 phút để khởi động.

### Cách 2 – Chạy để phát triển

```bash
# 1. SQL Server (cổng 1435) và Keycloak (cổng 8080)
docker compose -f docker-compose.dev.yml up -d

# 2. API: tạo tệp cấu hình chứa chuỗi kết nối (tệp này không đưa lên GitHub)
cp src/Hlt.Api/appsettings.Development.example.json src/Hlt.Api/appsettings.Development.json
#    rồi sửa Password= trong tệp cho đúng MSSQL_SA_PASSWORD trong .env
dotnet run --project src/Hlt.Api            # http://localhost:5080

# 3. Giao diện
cd web
npm install
npm run dev                                  # http://localhost:5173
```

Cơ sở dữ liệu được tạo tự động và nạp dữ liệu mẫu khi API khởi động lần đầu.

## Tài khoản dùng thử

Các tài khoản dưới đây là tài khoản **demo** được nạp sẵn trong realm `hlt01`, chỉ dùng để chạy thử trên máy cá nhân.

| Tên đăng nhập | Vai trò | Mật khẩu |
|---|---|---|
| benhnhan1 | Bệnh nhân | 123456 |
| nhanvien1 | Nhân viên tiếp đón | 123456 |
| bacsi1 | Bác sĩ (Khoa Nội) | 123456 |
| bacsi2 | Bác sĩ (Khoa Nhi) | 123456 |
| admin1 | Quản trị viên | 123456 |

Có thể tự đăng ký tài khoản bệnh nhân mới ở trang đăng nhập.

## Kiểm thử

```bash
dotnet test tests/Hlt.Domain.Tests          # kiểm thử đơn vị lớp miền
node tools/concurrency-test.mjs <maKhungGio> 10   # 10 yêu cầu đặt cùng một khung giờ (cần hệ thống đang chạy)
```

## Tài liệu

Sơ đồ UML, ERD, thiết kế giao diện và bảng biểu của báo cáo nằm trong thư mục [IMAGES](IMAGES).
