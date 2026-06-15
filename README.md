# UTE Parking System

Ứng dụng quản lý bãi xe trường HCMUTE, xây dựng bằng .NET MAUI.

## Công nghệ
- .NET MAUI (C#) — Windows / Android
- MVVM + WeakReferenceMessenger (real-time sync)
- SharedState pattern

## Tính năng
- Đăng nhập phân quyền: Sinh viên / Bảo vệ
- Sinh viên: chọn khu bãi, nhận chỗ tự động, vé QR, lấy xe, lịch sử
- Bảo vệ: sơ đồ bãi thời gian thực, khóa cổng, tìm xe, thống kê, điều chỉnh giá

## 👥 Phân Công Nhiệm Vụ Thành Viên

Dự án được phân chia theo các module độc lập để đảm bảo khối lượng công việc đồng đều và tối ưu hóa thế năng của từng thành viên:

| Thành viên | Phân hệ đảm nhiệm (Module) | Tệp mã nguồn (Source Code) | Chi tiết nghiệp vụ (Logic & UI) |
| :--- | :--- | :--- | :--- |
| **Trang** | **Core System & Shared Data**<br>*(Nền tảng & Dữ liệu dùng chung)* | `SplashPage.xaml.cs`<br>`LoginPage.xaml.cs`<br>`FileHandlingService.cs`<br>`SharedState.cs` | • Xử lý màn hình chờ (Splash) và giao diện Đăng nhập.<br>• Viết logic đọc dữ liệu tài khoản từ file Excel bằng thư viện `ExcelDataReader`.<br>• Quản lý Kho dữ liệu tập trung (`SharedState`), số liệu tổng và logic kiểm tra giờ hoạt động của bãi xe. |
| **Kính** | **Student App - Check-in Flow**<br>*(Phân hệ Sinh viên - Luồng Gửi xe)* | `StudentDashboard.xaml.cs`<br>*(Phần Khởi tạo & Tab 0)* | • Khởi tạo dữ liệu Sinh viên, chạy Timer đếm giờ thực và thống kê UI.<br>• Xử lý luồng tương tác: Chọn loại xe, Chọn Khu bãi (A, B, C, D) và Validate dữ liệu.<br>• Xây dựng thuật toán tự động quét và cấp phát chỗ trống (`FindNextSlot`).<br>• Lắng nghe và đồng bộ tín hiệu realtime từ Guard. |
| **Trung** | **Student App - Check-out Flow**<br>*(Phân hệ Sinh viên - Lấy xe & Lưu trữ)* | `StudentDashboard.xaml.cs`<br>*(Phần Map, Lấy xe & File)* | • Viết thuật toán vòng lặp vẽ sơ đồ ma trận bãi xe (`RenderMap`).<br>• Code luồng lấy xe: Tính toán thời gian (`TimeSpan`), chốt phí và giải phóng chỗ đỗ.<br>• Render giao diện thẻ Lịch sử (`AddHistoryCard`).<br>• Xử lý lưu phiên gửi xe ngầm (`Preferences`) và xuất mã QR ra file ảnh vật lý. |
| **Trâm** | **Guard App - Monitoring**<br>*(Phân hệ Bảo vệ - Giám sát bãi xe)* | `GuardDashboard.xaml.cs`<br>*(Phần Map & Tìm kiếm)* | • Code thuật toán sinh sơ đồ bản đồ động giám sát cho Bảo vệ (`RenderMap`).<br>• Viết thuật toán lọc và tìm kiếm xe rẽ nhánh (Tìm theo Biển số hoặc Vị trí).<br>• Xử lý logic gạt công tắc Khóa/Mở bãi khẩn cấp (`LockToggle`) để điều phối luồng xe của toàn hệ thống. |
| **Nguyên** | **Guard App - Dashboard**<br>*(Phân hệ Bảo vệ - Thống kê & Cấu hình)* | `GuardDashboard.xaml.cs`<br>*(Phần Stats, Timer & Cấu hình)* | • Dựng thuật toán sinh UI dạng lưới cho Bảng lượng xe theo 17 khung giờ (`BuildHourlyTable`).<br>• Xử lý mô phỏng doanh thu và phân bổ tỷ lệ xe ảo tự động chạy ngầm.<br>• Viết logic kiểm tra giờ hệ thống để tự động đóng/mở bãi (`CheckAutoLockByTime`).<br>• Cấu hình giá vé và quản lý logic của Class Model UI (`ZoneItem`). |

---
*Ghi chú: Toàn bộ logic giao diện động (Dynamic UI) đều được xử lý trực tiếp bằng mã C# Code-Behind để đảm bảo khả năng tùy biến dữ liệu theo thời gian thực.*
