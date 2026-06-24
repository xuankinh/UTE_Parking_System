# 🚲 UTE Parking System

**Hệ thống quản lý bãi gửi xe thông minh** dành cho sinh viên và bảo vệ trường Đại học Sư phạm Kỹ thuật TP.HCM (HCMUTE), xây dựng trên .NET MAUI (C#) — hỗ trợ gửi/lấy xe bằng QR code, theo dõi sức chứa bãi xe theo thời gian thực, và bảng điều khiển vận hành cho bảo vệ.

> Đồ án nhóm môn học — phát triển bởi nhóm 5 thành viên: **Trang, Kính, Trung, Trâm, Nguyên**

---

## 📑 Mục lục

- [Tổng quan](#-tổng-quan)
- [Tính năng chính](#-tính-năng-chính)
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Cấu trúc thư mục](#-cấu-trúc-thư-mục)
- [Demo / Screenshots](#-demo--screenshots)
- [Cài đặt & chạy thử](#-cài-đặt--chạy-thử)
- [Bảng phân công nhiệm vụ](#-bảng-phân-công-nhiệm-vụ)
- [Hướng phát triển](#-hướng-phát-triển)

---

## 🎯 Tổng quan

UTE Parking giải quyết bài toán quản lý bãi xe trong trường học: sinh viên cần biết bãi nào còn chỗ, gửi xe nhanh bằng QR, còn bảo vệ cần một bảng điều khiển để theo dõi sức chứa, khóa/mở cổng khẩn cấp và điều chỉnh giá vé theo thời gian thực — **đồng bộ giữa 2 ứng dụng** (Student & Guard) thông qua cơ chế `SharedState` + `WeakReferenceMessenger`.

Ứng dụng có **2 vai trò**, dùng chung 1 codebase .NET MAUI:

| Vai trò | Mục đích |
|---|---|
| 🚴 **Sinh viên** | Đăng nhập bằng MSSV, chọn loại xe & khu bãi, gửi/lấy xe bằng QR, xem lịch sử, xem hồ sơ & số dư ví |
| 🛡️ **Bảo vệ** | Theo dõi tình trạng sức chứa 4 khu bãi, khóa/mở cổng khẩn cấp, xem thống kê doanh thu, tra cứu lịch sử ra/vào theo biển số hoặc MSSV, chỉnh giá vé |

---

## ✨ Tính năng chính

### Phía Sinh viên (Student App)
- Đăng nhập bằng MSSV / mật khẩu
- Chọn loại phương tiện (Xe đạp / Xe máy) — giá vé riêng theo loại
- Chọn khu bãi (A/B/C/D) với cảnh báo real-time:
  - ⚠️ Cảnh báo "sắp đầy" khi khu bãi gần hết chỗ, cho phép chọn tiếp hoặc đổi khu khác
  - 🔒 Chặn chọn khu đang bị khóa bảo trì
- Cấp chỗ gửi xe tự động (vd: `C2 — Khu A, Hàng C, Chỗ 2`) kèm sơ đồ trực quan của khu bãi
- Xuất **vé QR** chứa đầy đủ thông tin: vị trí, biển số, giờ vào, phí dự kiến, mã SV, hạn vé 12h — có thể lưu ảnh QR
- Quét QR để **lấy xe ra**, tự tính phí theo thời gian gửi thực tế
- Lịch sử gửi xe (vào — ra — phí — trạng thái thanh toán)
- Hồ sơ cá nhân: email, trường, số dư ví

### Phía Bảo vệ (Guard App)
- Dashboard tổng quan: tổng chỗ trống, % lấp đầy, số khu bãi
- Theo dõi tình trạng sức chứa 4 khu bãi theo thời gian thực (đồng bộ tức thì với app sinh viên)
- **Điều khiển barrier khẩn cấp**: khóa/mở cổng từng khu bãi để bảo trì/điều phối
- Thống kê: doanh thu hôm nay, lượt xe, biểu đồ doanh thu theo tuần, lượng xe theo khung giờ (theo từng khu)
- Tra cứu lịch sử ra/vào bãi theo **biển số xe** hoặc **MSSV**
- Điều chỉnh bảng giá vé (xe đạp / xe máy / phí phạt vượt giờ) — đồng bộ tức thì toàn hệ thống

---

## 🏗️ Kiến trúc hệ thống

### 1️⃣ Sơ đồ khối tổng quan (Class-level Architecture)

```mermaid
flowchart TB
    subgraph VIEWS["📄 VIEWS — Presentation Layer"]
        direction LR
        SP["SplashPage.xaml.cs"]
        LP["LoginPage.xaml.cs"]
        SD["StudentDashboard.xaml.cs\n(partial: CheckIn / CheckOut)"]
        GD["GuardDashboard.xaml.cs\n(partial: Monitoring / Operations)"]
    end

    subgraph SERVICES["⚙️ SERVICES — Business Logic"]
        direction LR
        SS[("SharedState.cs\nnguồn dữ liệu chung")]
        FH["FileHandlingService.cs\nđọc Excel SV · lưu ảnh QR"]
    end

    subgraph MODELS["🧱 MODELS — Data"]
        direction LR
        ZI["ZoneItem\n(sức chứa, trạng thái khóa, giá)"]
    end

    SP --> LP
    LP --> SD
    LP --> GD

    SD -- "FindNextSlot()" --> SS
    SD -- "AddHistoryCard(), Preferences" --> FH
    GD -- "RenderMap(), LockToggle()" --> SS

    SS --> ZI

    style VIEWS fill:#2563eb,color:#fff
    style SERVICES fill:#1e3a8a,color:#fff
    style MODELS fill:#0f172a,color:#fff
```

> `SharedState.cs` là **nguồn dữ liệu chung duy nhất** (in-memory), giữ danh sách `ZoneItem` của 4 khu bãi. Khi `StudentDashboard` gọi `FindNextSlot()` để gửi xe, hoặc `GuardDashboard` gọi `LockToggle()` để khóa cổng, cả hai đều đọc/ghi trực tiếp vào `SharedState` → mọi thay đổi hiển thị ngay trên cả 2 app mà không cần gọi lại API hay reload màn hình.

### 2️⃣ Luồng khởi động & điều hướng

```mermaid
flowchart LR
    A(["SplashPage"]) --> B(["LoginPage"])
    B --> C{"Đăng nhập\nthành công?"}
    C -- "Vai trò: Sinh viên" --> D(["StudentDashboard"])
    C -- "Vai trò: Bảo vệ" --> E(["GuardDashboard"])
    C -- "Sai MSSV/mật khẩu" --> B

    style A fill:#2563eb,color:#fff
    style B fill:#2563eb,color:#fff
    style D fill:#16a34a,color:#fff
    style E fill:#ea580c,color:#fff
```

### 3️⃣ Module Student App (`StudentDashboard.xaml.cs`)

```mermaid
flowchart TB
    SD(["StudentDashboard.xaml.cs"]) --> T1["Tab: Gửi xe"]
    SD --> T2["Tab: Vé xe"]
    SD --> T3["Tab: Lịch sử"]
    SD --> T4["Tab: Hồ sơ"]

    T1 --> T1a["Chọn loại xe + khu bãi"]
    T1a -- "FindNextSlot()" --> T1b["Cấp chỗ tự động"]
    T1b --> T1c["Tạo vé QR"]

    T2 --> T2a["Hiển thị vé QR gửi xe"]
    T2 --> T2b["Quét QR lấy xe"]

    T3 -- "AddHistoryCard()" --> T3a["Danh sách lịch sử gửi/lấy"]

    T4 -- "Preferences" --> T4a["Lưu/khôi phục phiên gửi xe"]

    style SD fill:#2563eb,color:#fff
```

### 4️⃣ Module Guard App (`GuardDashboard.xaml.cs`)

```mermaid
flowchart TB
    GD(["GuardDashboard.xaml.cs"]) --> U1["Tab: Bãi đỗ"]
    GD --> U2["Tab: Thống kê"]
    GD --> U3["Tab: Lịch sử"]
    GD --> U4["Tab: Điều hành"]

    U1 -- "RenderMap()" --> U1a["Sơ đồ trực quan + sức chứa 4 khu"]

    U2 --> U2a["Doanh thu hôm nay"]
    U2 --> U2b["Biểu đồ tuần + khung giờ"]

    U3 --> U3a["Tra cứu theo biển số / MSSV"]

    U4 -- "LockToggle()" --> U4a["Khóa/mở cổng khẩn cấp"]
    U4 --> U4b["Điều chỉnh giá vé"]

    style GD fill:#ea580c,color:#fff
```

---

## 🛠️ Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | .NET MAUI (.NET 10, đa nền tảng Android/Windows) |
| Ngôn ngữ | C# (partial class theo module để 5 người làm song song) |
| Kiến trúc | MVVM, `SharedState` (state dùng chung), `WeakReferenceMessenger` (pub/sub) |
| UI | XAML, theme sáng — bảng màu mint-teal/xanh dương, card-based layout |
| QR Code | Sinh & quét QR cho vé gửi/lấy xe |
| Build mobile | `net10.0-android` (yêu cầu JDK + Android SDK qua Android Studio) |

---

## 📂 Cấu trúc thư mục

```
UTE_Parking_System/
├── Models/              # ParkingSlot, Zone, Ticket, User, PricingRule...
├── Services/            # ParkingService, AuthService, PricingService, SharedState
├── Views/               # Các trang XAML: Login, StudentDashboard, GuardDashboard...
├── Platforms/           # Cấu hình riêng theo platform (Android, Windows)
├── Resources/
│   ├── Screenshots/     # Ảnh chụp màn hình demo (xem mục Screenshots)
│   ├── Fonts, Images... 
├── Properties/
├── App.xaml / App.xaml.cs
├── AppShell.xaml / AppShell.xaml.cs
├── MainPage.xaml / MainPage.xaml.cs
├── MauiProgram.cs
└── UTE_Parking_Project.csproj
```

---

## 📸 Demo / Screenshots

> Đặt toàn bộ ảnh dưới đây vào `Resources/Screenshots/` trước khi push để các link ảnh render đúng trên GitHub.

### 1️⃣ Splash & Đăng nhập

| Splash Screen | Login Screen |
|---|---|
| ![Splash](Resources/Screenshots/SplashScreen.png) | ![Login](Resources/Screenshots/LoginScreen.png) |

### 2️⃣ Student App — Gửi xe

| Dashboard Gửi xe | Chọn khu bãi |
|---|---|
| ![Dashboard](Resources/Screenshots/StudentDashboard_GuiXe.png) | ![ChonKhuBai](Resources/Screenshots/GuiXe_ChonKhuBai.png) |

| Cảnh báo khu sắp đầy | Khu đang khóa bảo trì |
|---|---|
| ![Warning](Resources/Screenshots/GuiXe_KhuBSapDay_Warning.png) | ![Locked](Resources/Screenshots/GuiXe_KhuDDaKhoa_Warning.png) |

| Sơ đồ khu bãi | Nhập biển số xe |
|---|---|
| ![SoDo](Resources/Screenshots/GuiXe_SoDoKhuB.png) | ![BienSo](Resources/Screenshots/GuiXe_BienSoXeMay.png) |

| Gửi xe thành công | Khu A/B đã khóa → chọn Khu D |
|---|---|
| ![ThanhCong](Resources/Screenshots/GuiXe_ThanhCong.png) | ![ChonKhuD](Resources/Screenshots/GuiXe_KhuABDaKhoa_ChonKhuD.png) |

### 3️⃣ Student App — Vé xe / Lịch sử / Hồ sơ

| Vé QR gửi xe | Vé QR khu D |
|---|---|
| ![VeQR](Resources/Screenshots/VeXe_QRCode.png) | ![VeKhuD](Resources/Screenshots/VeXe_KhuD_QRCode.png) |

| Lưu ảnh vé QR | QR lấy xe |
|---|---|
| ![SaveQR](Resources/Screenshots/VeXe_SaveQR_FileExplorer.png) | ![LayXe](Resources/Screenshots/LayXe_QRCode.png) |

| Lịch sử gửi xe | Hồ sơ sinh viên |
|---|---|
| ![LichSu](Resources/Screenshots/LichSuGuiXe.png) | ![HoSo](Resources/Screenshots/HoSoSinhVien.png) |

### 4️⃣ Guard App — Bãi đỗ & Điều hành

| Bãi đỗ — tổng quan | Khóa cổng khẩn cấp |
|---|---|
| ![BaiDo](Resources/Screenshots/GuardDashboard_BaiDo.png) | ![KhoaCong](Resources/Screenshots/GuardDashboard_KhoaCongKhuAB.png) |

| Điều chỉnh giá vé | — |
|---|---|
| ![GiaVe](Resources/Screenshots/GuardDashboard_DieuHanh_GiaVe.png) | |

### 5️⃣ Guard App — Thống kê & Lịch sử

| Thống kê doanh thu | Lượng xe theo khung giờ |
|---|---|
| ![DoanhThu](Resources/Screenshots/GuardDashboard_ThongKe_DoanhThu.png) | ![KhungGio](Resources/Screenshots/GuardDashboard_ThongKe_LuongXeKhungGio.png) |

| Lịch sử (chưa tra cứu) | Tra cứu theo biển số |
|---|---|
| ![LichSuEmpty](Resources/Screenshots/GuardDashboard_LichSu_Empty.png) | ![TimBienSo](Resources/Screenshots/GuardDashboard_LichSu_TimBienSo.png) |

| Tra cứu theo MSSV | — |
|---|---|
| ![TimMaSV](Resources/Screenshots/GuardDashboard_LichSu_TimMaSV.png) | |

---

## 🚀 Cài đặt & chạy thử

```bash
git clone https://github.com/xuankinh/UTE_Parking_System.git
cd UTE_Parking_System

# Yêu cầu: .NET 10 SDK, MAUI workload
dotnet workload install maui

# Build & chạy trên Windows
dotnet build -t:Run -f net10.0-windows10.0.19041.0

# Build cho Android (cần Android SDK/JDK — cài qua Android Studio)
dotnet build -t:Run -f net10.0-android
```

---

## 👥 Bảng phân công nhiệm vụ

| Họ tên | Phụ trách | Mô tả chức năng |
|---|---|---|
| **Trang** | Login/Splash/Shared | Xây dựng màn hình đăng nhập, splash khởi động, xác thực tài khoản và kho dữ liệu chung của toàn hệ thống |
| **Kính** | Student (Tab Gửi xe) | Xây dựng tab Gửi xe: chọn loại xe, chọn khu, cấp chỗ tự động, vẽ bản đồ sơ đồ chỗ đỗ, và hạ tầng nền (đồng hồ, đồng bộ dữ liệu, timer) |
| **Trung** | Student (Tab Vé xe + Hồ sơ) | Xây dựng tab Vé xe và Hồ sơ: xác nhận lấy xe, lưu lịch sử, lưu ảnh vé QR, lưu/khôi phục phiên gửi xe qua Preferences, điều hướng tab và đăng xuất |
| **Trâm** | Guard (Khởi tạo + Map) | Khởi tạo màn hình Guard, xây dựng bản đồ sơ đồ bãi xe, xử lý chọn khu và mô hình dữ liệu khu (ZoneItem) |
| **Nguyên** | Guard (Clock + Nghiệp vụ) | Xử lý đồng hồ thời gian thực, thống kê doanh thu theo giờ, tự động khóa/mở bãi, khóa khu, tìm xe, cấu hình giá, điều hướng tab và đăng xuất |

---

## 🔭 Hướng phát triển

- Tích hợp backend thật (API/DB) thay cho `SharedState` in-memory
- Thanh toán qua ví điện tử thật / liên kết ngân hàng
- Camera AI nhận diện biển số tự động tại cổng (thay quét QR thủ công)
- Thông báo push khi bãi gần đầy hoặc xe quá hạn gửi
- Dashboard thống kê nâng cao cho phòng quản lý (không chỉ bảo vệ ca trực)

---

<p align="center">UTE Parking System — Đồ án nhóm, HCMUTE 2026</p>
