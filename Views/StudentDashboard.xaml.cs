using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace UTE_Parking_Project.Views;

public partial class StudentDashboard : ContentPage
{
    private readonly string _fullName;
    private readonly string _mssv;
    private readonly string _email;

    // ══ CẤU HÌNH CHỈ 4 KHU A, B, C, D (không có E) ══
    private readonly Dictionary<string, (int Total, int Cols)> _zoneConfig = new()
    {
        { "A", (100, 10) },
        { "B", (50,  10) },
        { "C", (60,  10) },
        { "D", (80,  10) },
    };

    // ══ LỐI VÀO/RA RIÊNG TỪNG KHU ══
    // Mỗi khu có vị trí lối vào và lối ra khác nhau trong bản đồ
    private readonly Dictionary<string, (string Entry, string Exit, string CardReader)> _zoneGates = new()
    {
        // Khu A: lối vào trên cùng bên trái, lối ra dưới bên phải, quẹt thẻ cổng A
        { "A", ("▲ CỔNG VÀO", "▼ CỔNG RA", "COL_0_CARD") },
        // Khu B: lối vào bên trái giữa, lối ra bên phải, quẹt thẻ cổng giữa B
        { "B", ("◀ CỔNG VÀO", "▶ CỔNG RA", "ROW_MID_CARD") },
        // Khu C: lối vào dưới bên trái, lối ra trên bên phải, quẹt thẻ cổng C
        { "C", ("▲ CỔNG VÀO", "▼ CỔNG RA", "COL_LAST_CARD") },
        // Khu D: lối vào trên bên phải, lối ra dưới bên trái, quẹt thẻ cổng D
        { "D", ("▲ CỔNG VÀO", "▼ CỔNG RA", "ROW_0_CARD") },
    };

    private readonly HashSet<string> _takenSlots = new();
    private readonly Random _rng = new Random();
    private string? _autoSlotId   = null;
    private string  _autoSlotText = "";
    private string  _currentZone  = "";
    private bool    _mapVisible   = false;

    // ══ TRẠNG THÁI GỬI XE ══
    private DateTime _parkInTime;
    private bool _isXeDap = true;
    private bool _hasParked = false;

    // ══ VÍ & LỊCH SỬ ══
    private int _soDuVi   = 45_000;
    private int _finalFee = 0;

    private record ParkingRecord(
        string Zone, string Slot, string Vehicle, string BienSo,
        DateTime InTime, DateTime OutTime, int Fee);
    private readonly List<ParkingRecord> _parkingHistory = new();

    private System.Timers.Timer? _clockTimer;
    private static readonly string[] RowLetters = "ABCDEFGHIJ".Select(c => c.ToString()).ToArray();

    private readonly HttpClient _httpClient = new();

    public StudentDashboard(string fullName, string mssv, string email = "an.22119159@hcmute.edu.vn")
    {
        InitializeComponent();

        _fullName = fullName;
        _mssv     = mssv;
        _email    = email;

        // ── Header: tên sinh viên căn giữa ──
        lblTenTab1.Text    = fullName;
        lblMssvHeader.Text = $"MSSV: {mssv}";

        // ── Tab hồ sơ ──
        lblTen.Text          = fullName;
        lblMssv.Text         = mssv;
        lblEmailProfile.Text = email;
        lblMaSvVe.Text       = mssv;

        // Avatar: 2 chữ đầu
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        lblAvatar.Text = parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : (fullName.Length >= 2 ? fullName[..2].ToUpper() : "SV");

        // Xe đạp mặc định -> ẩn biển số
        panelBienSo.IsVisible = false;

        StartTimers();
        InitSimulatedTakenSlots();
        UpdateAllZoneCards();
    }

    // ══════════════════════════════════════════════════
    //  ĐỒNG HỒ & RANDOM CHỈ SỐ LIÊN TỤC
    // ══════════════════════════════════════════════════
    private void StartTimers()
    {
        UpdateClockLabels();
        RandomizeStats(); // Khởi tạo lần đầu

        _clockTimer = new System.Timers.Timer(1000);
        _clockTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateClockLabels();
                // Random mỗi 5 giây để mô phỏng xe vào/ra liên tục
                if (DateTime.Now.Second % 5 == 0) RandomizeStats();
            });
        };
        _clockTimer.Start();
    }

    private void UpdateClockLabels()
    {
        var now = DateTime.Now;
        lblClock.Text = now.ToString("HH:mm:ss");
        lblDate.Text  = now.ToString("ddd, d/M/yyyy");
    }

    private void RandomizeStats()
    {
        // Tổng 290 chỗ (100+50+60+80). Lấy số thực tế từ các khu
        int realTaken = _takenSlots.Count;
        int totalAll  = _zoneConfig.Values.Sum(z => z.Total);
        int free      = Math.Max(0, totalAll - realTaken + _rng.Next(-5, 6)); // +/- dao động nhỏ
        free = Math.Clamp(free, 0, totalAll);
        int pct = (int)(100.0 * (totalAll - free) / totalAll);

        lblStatFree.Text = free.ToString();
        lblStatFull.Text = $"{pct}%";
        lblStatZone.Text = _zoneConfig.Count.ToString();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
    }

    // ══════════════════════════════════════════════════
    //  GIẢ LẬP DỮ LIỆU XE TRONG BÃI
    // ══════════════════════════════════════════════════
    private void InitSimulatedTakenSlots()
    {
        // Khu A: 20/100 đã có xe (còn nhiều chỗ)
        for (int c = 1; c <= 10; c++) _takenSlots.Add($"A-1-{c}");
        for (int c = 1; c <= 10; c++) _takenSlots.Add($"A-2-{c}");

        // Khu B: 46/50 có xe (sắp đầy - còn 4 chỗ)
        for (int r = 1; r <= 4; r++)
            for (int c = 1; c <= 10; c++) _takenSlots.Add($"B-{r}-{c}");
        for (int c = 1; c <= 6; c++) _takenSlots.Add($"B-5-{c}");

        // Khu C: 58/60 có xe (sắp đầy - còn 2 chỗ)
        for (int r = 1; r <= 5; r++)
            for (int c = 1; c <= 10; c++) _takenSlots.Add($"C-{r}-{c}");
        for (int c = 1; c <= 8; c++) _takenSlots.Add($"C-6-{c}");

        // Khu D: 37/80 có xe (còn nhiều)
        for (int r = 1; r <= 3; r++)
            for (int c = 1; c <= 10; c++) _takenSlots.Add($"D-{r}-{c}");
        for (int c = 1; c <= 7; c++) _takenSlots.Add($"D-4-{c}");
    }

    // ══════════════════════════════════════════════════
    //  CHỌN LOẠI XE
    // ══════════════════════════════════════════════════
    private void OnXeDapClicked(object? sender, EventArgs e)
    {
        _isXeDap = true;

        // Khung xanh cho Xe đạp
        borderXeDap.Stroke          = Color.FromArgb("#1D4ED8");
        borderXeDap.StrokeThickness = 2.5;
        borderXeDap.BackgroundColor = Color.FromArgb("#EFF6FF");
        lblXeDapText.TextColor      = Color.FromArgb("#1D4ED8");

        // Xám cho Xe máy
        borderXeMay.Stroke          = Color.FromArgb("#E2E8F0");
        borderXeMay.StrokeThickness = 1.5;
        borderXeMay.BackgroundColor = Colors.White;
        lblXeMayText.TextColor      = Color.FromArgb("#94A3B8");

        lblIconVehicle.Text = "🚲";
        lblIconVe.Text      = "🚲";

        // Xe đạp: KHÔNG cần biển số
        panelBienSo.IsVisible = false;
    }

    private void OnXeMayClicked(object? sender, EventArgs e)
    {
        _isXeDap = false;

        // Khung xanh cho Xe máy
        borderXeMay.Stroke          = Color.FromArgb("#1D4ED8");
        borderXeMay.StrokeThickness = 2.5;
        borderXeMay.BackgroundColor = Color.FromArgb("#EFF6FF");
        lblXeMayText.TextColor      = Color.FromArgb("#1D4ED8");

        // Xám cho Xe đạp
        borderXeDap.Stroke          = Color.FromArgb("#E2E8F0");
        borderXeDap.StrokeThickness = 1.5;
        borderXeDap.BackgroundColor = Colors.White;
        lblXeDapText.TextColor      = Color.FromArgb("#94A3B8");

        lblIconVehicle.Text = "🛵";
        lblIconVe.Text      = "🛵";

        // Xe máy: CẦN biển số
        panelBienSo.IsVisible = true;
    }

    // ══════════════════════════════════════════════════
    //  CẬP NHẬT CARD KHU BÃI
    // ══════════════════════════════════════════════════
    private void UpdateAllZoneCards()
    {
        UpdateZoneCard("A", borderZoneA, lblTitleZoneA, lblBadgeA, lblSlotA, barA);
        UpdateZoneCard("B", borderZoneB, lblTitleZoneB, lblBadgeB, lblSlotB, barB);
        UpdateZoneCard("C", borderZoneC, lblTitleZoneC, lblBadgeC, lblSlotC, barC);
        UpdateZoneCard("D", borderZoneD, lblTitleZoneD, lblBadgeD, lblSlotD, barD);

        // Cập nhật stat header
        RandomizeStats();
    }

    private void UpdateZoneCard(string zone, Border border, Label title, Label badge, Label slot, Border bar)
    {
        int total = _zoneConfig[zone].Total;
        int taken = _takenSlots.Count(s => s.StartsWith($"{zone}-"));
        int free  = total - taken;
        double pct = (double)taken / total;

        slot.Text = $"{free} / {total} chỗ trống";

        // Bar width tỉ lệ tối đa 120px
        bar.WidthRequest = Math.Max(4, (int)(120 * pct));

        if (free == 0)
        {
            // HẾT CHỖ: đỏ, DISABLE click
            border.Stroke          = Color.FromArgb("#FECACA");
            border.StrokeThickness = 2;
            badge.Text             = "Hết chỗ";
            badge.TextColor        = Color.FromArgb("#DC2626");
            bar.BackgroundColor    = Color.FromArgb("#EF4444");
            border.Opacity         = 0.6; // Làm mờ thể hiện disabled
        }
        else if (free <= 5)
        {
            // CẢNH BÁO: cam, VẪN cho chọn (chỉ cảnh báo)
            border.Stroke          = Color.FromArgb("#FDE68A");
            border.StrokeThickness = 1.5;
            badge.Text             = "Sắp đầy";
            badge.TextColor        = Color.FromArgb("#D97706");
            bar.BackgroundColor    = Color.FromArgb("#F59E0B");
            border.Opacity         = 1.0;
        }
        else
        {
            // CÒN CHỖ: xanh
            border.Stroke          = Color.FromArgb("#E2E8F0");
            border.StrokeThickness = 1.5;
            badge.Text             = "Còn chỗ";
            badge.TextColor        = Color.FromArgb("#059669");
            bar.BackgroundColor    = Color.FromArgb("#10B981");
            border.Opacity         = 1.0;
        }
    }

    private void OnZoneAClicked(object? sender, EventArgs e) => SelectZone("A", borderZoneA);
    private void OnZoneBClicked(object? sender, EventArgs e) => SelectZone("B", borderZoneB);
    private void OnZoneCClicked(object? sender, EventArgs e) => SelectZone("C", borderZoneC);
    private void OnZoneDClicked(object? sender, EventArgs e) => SelectZone("D", borderZoneD);

    private Border? _lastSelectedBorder;

    private async void SelectZone(string zone, Border clickedBorder)
    {
        int total = _zoneConfig[zone].Total;
        int taken = _takenSlots.Count(s => s.StartsWith($"{zone}-"));
        int free  = total - taken;

        // CHỈ chặn khi HẾT HOÀN TOÀN (free == 0)
        if (free == 0)
        {
            await DisplayAlertAsync("Hết chỗ!", $"Khu {zone} đã hết chỗ 🚫\nVui lòng chọn khu khác.", "Đóng");
            return;
        }

        // Cảnh báo nhẹ khi sắp đầy (còn 1-5 chỗ) nhưng VẪN cho vào
        if (free <= 5)
        {
            bool confirm = await DisplayAlertAsync(
                $"⚠ Khu {zone} sắp đầy!",
                $"Chỉ còn {free} chỗ trống.\nBạn có muốn gửi ở đây không?",
                "Vẫn chọn", "Chọn khu khác");
            if (!confirm) return;
        }

        // ── Bỏ highlight cũ ──
        if (_lastSelectedBorder != null)
        {
            // Khôi phục màu theo trạng thái khu
            UpdateAllZoneCards();
        }

        // ── Highlight khung đã chọn (xanh đậm) ──
        clickedBorder.Stroke          = Color.FromArgb("#2563EB");
        clickedBorder.StrokeThickness = 3;
        _lastSelectedBorder           = clickedBorder;

        _currentZone = zone;
        var nextSlot = FindNextSlot(zone);

        if (nextSlot != null)
        {
            _autoSlotId   = $"{zone}-{nextSlot.Value.R}-{nextSlot.Value.C}";
            _autoSlotText = $"{RowLetters[nextSlot.Value.R - 1]}{nextSlot.Value.C}";

            lblViTriDuocCap.Text = _autoSlotText;
            lblViTriDetail.Text  = $"Khu {zone} — Hàng {RowLetters[nextSlot.Value.R - 1]}, Chỗ {nextSlot.Value.C}";
            lblConTrong.Text     = (free - 1).ToString();
            lblDaDo.Text         = (taken + 1).ToString();

            // Bật nút xác nhận
            btnThanhToan.IsEnabled = true;
            btnThanhToan.Opacity   = 1;
        }

        panelViTri.IsVisible = true;

        // Đóng map cũ, reset
        _mapVisible           = false;
        panelMap.IsVisible    = false;
        lblToggleMap.Text     = "Mở map";
        lblSoDoBaiTitle.Text  = $"Sơ đồ Khu {zone}";
    }

    private (int R, int C)? FindNextSlot(string zone)
    {
        var cfg  = _zoneConfig[zone];
        int rows = cfg.Total / cfg.Cols;
        for (int r = 1; r <= rows; r++)
            for (int c = 1; c <= cfg.Cols; c++)
                if (!_takenSlots.Contains($"{zone}-{r}-{c}")) return (r, c);
        return null;
    }

    // ══════════════════════════════════════════════════
    //  BẢN ĐỒ SƠ ĐỒ — LỐI VÀO/RA VÀ KHU VỰC CỔNG TUẦN TỰ
    // ══════════════════════════════════════════════════
    private void OnToggleMapClicked(object? sender, EventArgs e)
    {
        _mapVisible        = !_mapVisible;
        panelMap.IsVisible = _mapVisible;
        lblToggleMap.Text  = _mapVisible ? "Đóng" : "Mở map";
        if (_mapVisible) RenderMap(_currentZone, _autoSlotId);
    }

    // ===== PHƯƠNG THỨC MỚI 1: RenderMap (Tích hợp chuẩn chỉ) =====
    private void RenderMap(string zone, string? highlightId)
    {
        var cfg   = _zoneConfig[zone];
        int rows  = cfg.Total / cfg.Cols;
        var gates = _zoneGates[zone];

        slotRows.Children.Clear();
        slotColHeader.Children.Clear();

        if (zone == "A")
        {
            slotRows.Children.Add(CreateGateRow("▲ CỔNG VÀO", LayoutOptions.Start, "#1D4ED8"));
            RenderSlotRows(zone, rows, cfg.Cols, highlightId);
            slotRows.Children.Add(CreateGateRow("▼ CỔNG RA", LayoutOptions.End, "#DC2626"));
        }
        else if (zone == "B")
        {
            slotRows.Children.Add(CreateGateRow("◀ CỔNG VÀO", LayoutOptions.Start, "#1D4ED8"));
            int midRow = rows / 2;
            RenderSlotRowsRange(zone, 1, midRow, cfg.Cols, highlightId);
            RenderSlotRowsRange(zone, midRow + 1, rows, cfg.Cols, highlightId);
            slotRows.Children.Add(CreateGateRow("▶ CỔNG RA", LayoutOptions.End, "#DC2626"));
        }
        else if (zone == "C")
        {
            slotRows.Children.Add(CreateGateRow("▲ CỔNG VÀO", LayoutOptions.Center, "#1D4ED8"));
            RenderSlotRows(zone, rows, cfg.Cols, highlightId);
            slotRows.Children.Add(CreateGateRow("▼ CỔNG RA", LayoutOptions.Start, "#DC2626"));
        }
        else if (zone == "D")
        {
            slotRows.Children.Add(CreateGateRow("▲ CỔNG VÀO", LayoutOptions.End, "#1D4ED8"));
            RenderSlotRows(zone, rows, cfg.Cols, highlightId);
            slotRows.Children.Add(CreateGateRow("▼ CỔNG RA", LayoutOptions.Start, "#DC2626"));
        }
    }

    private void RenderSlotRows(string zone, int rows, int cols, string? highlightId)
        => RenderSlotRowsRange(zone, 1, rows, cols, highlightId);

    private void RenderSlotRowsRange(string zone, int fromRow, int toRow, int cols, string? highlightId)
    {
        for (int r = fromRow; r <= toRow; r++)
        {
            var rowGrid = new Grid { ColumnSpacing = 4 };
            for (int c = 1; c <= cols; c++)
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 34 });

            for (int c = 1; c <= cols; c++)
            {
                string id      = $"{zone}-{r}-{c}";
                bool isTaken   = _takenSlots.Contains(id);
                bool isMe      = id == highlightId;
                string label   = isMe ? "BẠN" : (isTaken ? "×" : $"{RowLetters[r-1]}{c}");

                var box = new Border
                {
                    HeightRequest   = 30,
                    BackgroundColor = isMe    ? Color.FromArgb("#FDE047")
                                    : isTaken ? Color.FromArgb("#CBD5E1")
                                    : Colors.White,
                    Stroke          = isMe    ? Color.FromArgb("#CA8A04")
                                    : Color.FromArgb("#E2E8F0"),
                    StrokeThickness = isMe ? 2 : 1,
                    StrokeShape     = new RoundRectangle { CornerRadius = 5 },
                    Content         = new Label
                    {
                        Text              = label,
                        FontSize          = 8,
                        FontAttributes    = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions   = LayoutOptions.Center,
                        TextColor         = isMe    ? Color.FromArgb("#78350F")
                                          : isTaken ? Color.FromArgb("#94A3B8")
                                          : Color.FromArgb("#64748B")
                    }
                };
                Grid.SetColumn(box, c - 1);
                rowGrid.Children.Add(box);
            }
            slotRows.Children.Add(rowGrid);
        }
    }

    // ===== PHƯƠNG THỨC MỚI 2: CreateGateRow (Tích hợp gọn gàng) =====
    private Border CreateGateRow(string text, LayoutOptions align, string bgHex)
    {
        return new Border
        {
            BackgroundColor   = Color.FromArgb(bgHex),
            Padding           = new Thickness(10, 5),
            HorizontalOptions = align,
            Margin            = new Thickness(0, 3),
            StrokeShape       = new RoundRectangle { CornerRadius = new CornerRadius(7) },
            Content           = new Label
            {
                Text           = text,
                FontSize       = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor      = Colors.White
            }
        };
    }

    /// <summary>Tạo hàng chỉ điểm quẹt thẻ / QR</summary>
    private Border CreateCardReaderRow(string text, LayoutOptions align)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#DBEAFE"),
            Stroke          = Color.FromArgb("#3B82F6"),
            StrokeThickness = 1.5,
            Padding         = new Thickness(12, 5),
            HorizontalOptions = align,
            Margin          = new Thickness(0, 2),
            StrokeShape     = new RoundRectangle { CornerRadius = 8 },
            Content         = new Label
            {
                Text           = text,
                FontSize       = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor      = Color.FromArgb("#1D4ED8")
            }
        };
    }

    // ══════════════════════════════════════════════════
    //  XÁC NHẬN GỬI XE
    // ══════════════════════════════════════════════════
    private async void OnThanhToanClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_autoSlotId))
        {
            await DisplayAlertAsync("Lỗi", "Vui lòng chọn khu bãi trước!", "Đóng");
            return;
        }

        // Xe máy: bắt buộc nhập biển số
        if (!_isXeDap && string.IsNullOrWhiteSpace(txtBienSo.Text))
        {
            await DisplayAlertAsync("Lỗi", "Vui lòng nhập biển số xe máy!", "Đóng");
            return;
        }

        _hasParked   = true;
        _parkInTime  = DateTime.Now;

        string bienSo        = _isXeDap ? "Xe đạp (Không biển)" : txtBienSo.Text.Trim().ToUpper();
        int    estimatedFee  = _isXeDap ? 3000 : 5000;

        // ── Điền thông tin vé ──
        lblKhuBaiVe.Text  = $"Vé gửi xe — Khu {_currentZone}";
        lblSlot.Text      = _autoSlotText;
        lblBien.Text      = bienSo;
        lblGioGui.Text    = _parkInTime.ToString("HH:mm  •  dd/MM/yyyy");
        lblPhiGui.Text    = $"{estimatedFee:N0}đ (Dự kiến, trong 12h)";
        lblMaSvVe.Text    = _mssv;
        lblHanVe.Text     = "12 tiếng";
        lblIconVe.Text    = _isXeDap ? "🚲" : "🛵";

        // ── Tạo QR code ──
        string rawData = $"UTE-PARK|{_mssv}|{bienSo}|{_autoSlotId}|{_parkInTime:yyyyMMddHHmm}|{_currentZone}";
        string qrUrl   = $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&color=0F2D6B&bgcolor=FFFFFF&data={Uri.EscapeDataString(rawData)}";
        imgQR.Source       = qrUrl;
        imgQRLayXe.Source  = qrUrl;

        // ── Ghi nhận chỗ ──
        _takenSlots.Add(_autoSlotId);
        UpdateAllZoneCards();

        await DisplayAlertAsync("✅ Gửi xe thành công!",
            $"Vị trí: {_autoSlotText} — Khu {_currentZone}\nGiờ vào: {_parkInTime:HH:mm}\nPhí dự kiến: {estimatedFee:N0}đ",
            "Xem Vé");

        SwitchTab(1);
        OnSubVeGuiClicked(null, EventArgs.Empty);
    }

    // ══════════════════════════════════════════════════
    //  TAB VÉ XE / LẤY XE TRONG TRANG VÉ
    // ══════════════════════════════════════════════════
    private void OnSubVeGuiClicked(object? sender, EventArgs e)
    {
        panelVeGui.IsVisible = true;
        panelLayXe.IsVisible = false;
        btnSubVeGui.BackgroundColor = Color.FromArgb("#1D4ED8");
        btnSubVeGui.TextColor       = Colors.White;
        btnSubLayXe.BackgroundColor = Colors.Transparent;
        btnSubLayXe.TextColor       = Color.FromArgb("#94A3B8");
    }

    private void OnSubLayXeClicked(object? sender, EventArgs e)
    {
        if (!_hasParked)
        {
            _ = DisplayAlertAsync("Thông báo", "Bạn chưa gửi xe nào!", "Đóng");
            return;
        }

        panelVeGui.IsVisible = false;
        panelLayXe.IsVisible = true;
        btnSubLayXe.BackgroundColor = Color.FromArgb("#059669");
        btnSubLayXe.TextColor       = Colors.White;
        btnSubVeGui.BackgroundColor = Colors.Transparent;
        btnSubVeGui.TextColor       = Color.FromArgb("#94A3B8");

        lblSlotLayXe.Text    = _autoSlotText;
        lblGioVaoLayXe.Text  = _parkInTime.ToString("HH:mm (dd/MM)");

        // ── LOGIC TÍNH PHÍ CHÍNH XÁC THEO MỐC CHẶN 12H ──
        DateTime outTime  = DateTime.Now;
        TimeSpan duration = outTime - _parkInTime;
        // (Bỏ comment dòng dưới để kiểm tra điều kiện phạt khi cần)
        // TimeSpan duration = TimeSpan.FromHours(14); 

        lblGioRaLayXe.Text    = outTime.ToString("HH:mm");
        lblThoiGianGui.Text   = $"{(int)duration.TotalHours}g {duration.Minutes:D2}p";

        int finalFee;
        if (duration.TotalHours > 12)
        {
            // Quá 12 tiếng: phạt 10.000đ cho cả xe đạp lẫn xe máy
            finalFee = 10000;
            lblCanhBaoQuaGio.IsVisible = true;
        }
        else
        {
            // Trong 12 tiếng: xe máy 5k, xe đạp 3k
            finalFee = _isXeDap ? 3000 : 5000;
            lblCanhBaoQuaGio.IsVisible = false;
        }

        _finalFee = finalFee;
        lblTongPhiLayXe.Text = $"{finalFee:N0}đ";

        btnBorderXacNhan.IsVisible = true;
    }

    private async void OnXacNhanLayXeClicked(object? sender, EventArgs e)
    {
        if (_soDuVi < _finalFee)
        {
            await DisplayAlertAsync("❌ Không đủ số dư",
                $"Ví còn {_soDuVi:N0}đ, cần {_finalFee:N0}đ.\nVui lòng nạp thêm tiền.", "Đóng");
            return;
        }

        _soDuVi -= _finalFee;
        lblSoDuVi.Text = $"{_soDuVi:N0}đ";

        string bienSo = _isXeDap ? "Xe đạp" : (txtBienSo.Text?.Trim() ?? "Xe máy");
        var record = new ParkingRecord(
            Zone:    _currentZone,
            Slot:    _autoSlotText,
            Vehicle: _isXeDap ? "🚲 Xe đạp" : "🛵 Xe máy",
            BienSo:  bienSo,
            InTime:  _parkInTime,
            OutTime: DateTime.Now,
            Fee:     _finalFee);
        _parkingHistory.Insert(0, record);
        AddHistoryCard(record);

        btnBorderXacNhan.IsVisible = false;

        _takenSlots.Remove(_autoSlotId!);
        _hasParked          = false;
        _autoSlotId         = null;
        _autoSlotText       = "";
        _currentZone        = "";
        _finalFee           = 0;
        _lastSelectedBorder = null;
        UpdateAllZoneCards();

        panelViTri.IsVisible   = false;
        panelBienSo.IsVisible  = false;
        btnThanhToan.IsEnabled = false;
        btnThanhToan.Opacity   = 0.5;
    }

    private void AddHistoryCard(ParkingRecord r)
    {
        lblEmptyHistory.IsVisible = false;

        TimeSpan dur     = r.OutTime - r.InTime;
        string   durText = $"{(int)dur.TotalHours}g {dur.Minutes:D2}p";

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding         = new Thickness(16, 14),
            Shadow          = new Shadow { Brush = Brush.Black, Opacity = 0.05f, Radius = 8, Offset = new Point(0, 3) }
        };
        card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) };

        var grid = new Grid
        {
            ColumnSpacing     = 12,
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new() { Width = GridLength.Auto },
                new() { Width = GridLength.Star },
                new() { Width = GridLength.Auto }
            }
        };

        var icon = new Label
        {
            Text            = r.Vehicle.StartsWith("🚲") ? "🚲" : "🛵",
            FontSize        = 28,
            VerticalOptions = LayoutOptions.Center
        };

        var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        info.Add(new Label
        {
            Text           = $"Khu {r.Zone} — Chỗ {r.Slot}",
            FontSize       = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor      = Color.FromArgb("#1E293B")
        });
        info.Add(new Label { Text = r.BienSo, FontSize = 12, TextColor = Color.FromArgb("#64748B") });
        info.Add(new Label
        {
            Text      = $"Vào {r.InTime:HH:mm}  →  Ra {r.OutTime:HH:mm}  ({durText})",
            FontSize  = 11,
            TextColor = Color.FromArgb("#94A3B8")
        });

        var feeStack = new VerticalStackLayout
        {
            Spacing           = 2,
            VerticalOptions   = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        feeStack.Add(new Label
        {
            Text              = $"{r.Fee:N0}đ",
            FontSize          = 15,
            FontAttributes    = FontAttributes.Bold,
            TextColor         = Color.FromArgb("#059669"),
            HorizontalOptions = LayoutOptions.End
        });
        feeStack.Add(new Label
        {
            Text              = "✓ Đã TT",
            FontSize          = 11,
            TextColor         = Color.FromArgb("#10B981"),
            HorizontalOptions = LayoutOptions.End
        });

        Grid.SetColumn(icon,     0);
        Grid.SetColumn(info,     1);
        Grid.SetColumn(feeStack, 2);
        grid.Add(icon);
        grid.Add(info);
        grid.Add(feeStack);

        card.Content = grid;
        stackLichSu.Children.Insert(0, card);
    }

    // ══════════════════════════════════════════════════
    //  LƯU VÉ QR (SỬ DỤNG CHỈ ĐỊNH TUYỆT ĐỐI System.IO.Path)
    // ══════════════════════════════════════════════════
    private async void OnLuuVeClicked(object? sender, EventArgs e)
    {
        if (imgQR.Source == null)
        {
            await DisplayAlertAsync("Lỗi", "Chưa có vé để lưu!", "Đóng");
            return;
        }

        try
        {
            btnLuuVe.Text      = "⏳ Đang tải...";
            btnLuuVe.IsEnabled = false;

            string rawData  = $"UTE-PARK|{_mssv}|{_autoSlotId}|{_parkInTime:yyyyMMddHHmm}";
            string qrUrl    = $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&color=0F2D6B&bgcolor=FFFFFF&data={Uri.EscapeDataString(rawData)}";

            byte[] imageBytes = await _httpClient.GetByteArrayAsync(qrUrl);

#if ANDROID || IOS || MACCATALYST
            string fileName   = $"UTE_Parking_Ve_{_mssv}_{DateTime.Now:yyyyMMdd_HHmm}.png";
            string tempPath   = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllBytesAsync(tempPath, imageBytes);
            await MediaPicker.CapturePhotoAsync(); 
            
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Lưu vé gửi xe UniPark",
                File  = new ShareFile(tempPath, "image/png")
            });
#else
            string fileName = $"UTE_Parking_Ve_{_mssv}_{DateTime.Now:yyyyMMdd_HHmm}.png";
            
            // Lấy thư mục chạy hiện tại của file .exe (đang nằm sâu trong bin/Debug/.../win-x64)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            
            // "Hack" đi ngược lên 4 cấp thư mục để thoát khỏi cấu trúc bin/Debug/... và về lại thư mục gốc dự án
            for (int i = 0; i < 4; i++)
            {
                if (dir.Parent != null) dir = dir.Parent;
            }
            
            // Lấy đường dẫn thư mục gốc chứa code (nơi có file UTE_Parking_Project.csproj)
            string projectRoot = dir.FullName;
            
            // Chỉ định lưu vào thư mục riêng tên là "SavedQRs" ngay trong hệ thống file code của ông
            string targetFolder = System.IO.Path.Combine(projectRoot, "SavedQRs");
            
            // Nếu thư mục "SavedQRs" chưa tồn tại trong bộ code, tự động tạo luôn cho ông
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            
            // Ghép nối tạo đường dẫn file tuyệt đối
            string savePath = System.IO.Path.Combine(targetFolder, fileName);
            
            // Ghi file ảnh vật lý vào thư mục code
            await File.WriteAllBytesAsync(savePath, imageBytes);
            
            await DisplayAlertAsync("✅ Đã lưu vào Project!", $"Vé QR đã được lưu thẳng vào thư mục code gốc:\nSavedQRs\\{fileName}", "Đóng");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Lỗi", $"Không thể lưu ảnh: {ex.Message}", "Đóng");
        }
        finally
        {
            btnLuuVe.Text      = "⬇  Lưu vé (Tải ảnh QR)";
            btnLuuVe.IsEnabled = true;
        }
    }

    // ══════════════════════════════════════════════════
    //  ENTRY EVENTS
    // ══════════════════════════════════════════════════
    private void OnBienSoFocused(object? sender, FocusEventArgs e)
    {
        borderBienSo.Stroke          = Color.FromArgb("#1D4ED8");
        borderBienSo.StrokeThickness = 2;
    }

    private void OnBienSoUnfocused(object? sender, FocusEventArgs e)
    {
        borderBienSo.Stroke          = Color.FromArgb("#E5E7EB");
        borderBienSo.StrokeThickness = 1.5;
        iconCheckBienSo.IsVisible = !string.IsNullOrWhiteSpace(txtBienSo.Text);
    }

    private void OnBienSoTextChanged(object? sender, TextChangedEventArgs e)
    {
        iconCheckBienSo.IsVisible = !string.IsNullOrWhiteSpace(e.NewTextValue);
    }

    private void OnBaiGuiChanged(object? sender, EventArgs e) { }

    // ══════════════════════════════════════════════════
    //  CHUYỂN TAB CỦA THANH TAB ĐẦU TRANG
    // ══════════════════════════════════════════════════
    private void OnTab0Clicked(object? sender, EventArgs e) => SwitchTab(0);
    private void OnTab1Clicked(object? sender, EventArgs e) => SwitchTab(1);
    private void OnTab2Clicked(object? sender, EventArgs e) => SwitchTab(2);
    private void OnTab3Clicked(object? sender, EventArgs e) => SwitchTab(3);

    // ===== PHƯƠNG THỨC MỚI 3: SwitchTab (Tích hợp chuẩn chỉ) =====
    private void SwitchTab(int index)
    {
        Tab0.IsVisible = Tab1.IsVisible = Tab2.IsVisible = Tab3.IsVisible = false;

        // Reset tất cả tabs: icon mờ, text nhạt, underline ẩn
        hIconTab0.Opacity = hIconTab1.Opacity = hIconTab2.Opacity = hIconTab3.Opacity = 0.55;
        hTextTab0.TextColor = hTextTab1.TextColor = hTextTab2.TextColor = hTextTab3.TextColor
            = Color.FromArgb("#93C5FD");
        hTabBg0.BackgroundColor = hTabBg1.BackgroundColor
            = hTabBg2.BackgroundColor = hTabBg3.BackgroundColor = Colors.Transparent;

        // Ẩn tất cả underline
        hTabLine0.Color = hTabLine1.Color = hTabLine2.Color = hTabLine3.Color = Colors.Transparent;

        var activeText = Colors.White;

        switch (index)
        {
            case 0:
                Tab0.IsVisible = true;
                hIconTab0.Opacity = 1;
                hTextTab0.TextColor = activeText;
                hTabLine0.Color = Colors.White;
                break;
            case 1:
                Tab1.IsVisible = true;
                hIconTab1.Opacity = 1;
                hTextTab1.TextColor = activeText;
                hTabLine1.Color = Colors.White;
                break;
            case 2:
                Tab2.IsVisible = true;
                hIconTab2.Opacity = 1;
                hTextTab2.TextColor = activeText;
                hTabLine2.Color = Colors.White;
                break;
            case 3:
                Tab3.IsVisible = true;
                hIconTab3.Opacity = 1;
                hTextTab3.TextColor = activeText;
                hTabLine3.Color = Colors.White;
                break;
        }
    }

    // ══════════════════════════════════════════════════
    //  ĐĂNG XUẤT
    // ══════════════════════════════════════════════════
    private async void OnDangXuatClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Đăng xuất", "Hủy");
        if (confirm)
        {
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            await Navigation.PopAsync();
        }
    }
}