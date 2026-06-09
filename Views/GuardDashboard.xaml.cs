using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace UTE_Parking_Project.Views;

public partial class GuardDashboard : ContentPage
{
    // Constructor mặc định (dùng khi test)
    public GuardDashboard() : this("Cán Bộ Vô Danh") { }

    // Constructor nhận tên thật từ màn hình Đăng Nhập
    public GuardDashboard(string guardName)
    {
        InitializeComponent();
        
        // Hiển thị tên lên Header và Profile
        lblGuardName.Text = $"Cán bộ: {guardName} • guard01";
        lblProfileName.Text = guardName;
    }

    // --- LOGIC CHUYỂN TAB ẢO ---
    private void OnTab0Clicked(object sender, EventArgs e) => SwitchTab(0);
    private void OnTab1Clicked(object sender, EventArgs e) => SwitchTab(1);
    private void OnTab2Clicked(object sender, EventArgs e) => SwitchTab(2);
    private void OnTab3Clicked(object sender, EventArgs e) => SwitchTab(3);
    private void OnTab4Clicked(object sender, EventArgs e) => SwitchTab(4);

    private void SwitchTab(int index)
    {
        // Ẩn tất cả
        Tab0.IsVisible = Tab1.IsVisible = Tab2.IsVisible = Tab3.IsVisible = Tab4.IsVisible = false;
        
        // Reset màu chữ Inactive (Xám)
        Color inactiveColor = Color.FromArgb("#94a3b8");
        iconTab0.Opacity = iconTab1.Opacity = iconTab2.Opacity = iconTab3.Opacity = iconTab4.Opacity = 0.4;
        textTab0.TextColor = textTab1.TextColor = textTab2.TextColor = textTab3.TextColor = textTab4.TextColor = inactiveColor;

        // Bật Tab Active (Đỏ bã trầu)
        Color activeColor = Color.FromArgb("#b91c1c");
        if (index == 0) { Tab0.IsVisible = true; iconTab0.Opacity = 1; textTab0.TextColor = activeColor; }
        if (index == 1) { Tab1.IsVisible = true; iconTab1.Opacity = 1; textTab1.TextColor = activeColor; }
        if (index == 2) { Tab2.IsVisible = true; iconTab2.Opacity = 1; textTab2.TextColor = activeColor; }
        if (index == 3) { Tab3.IsVisible = true; iconTab3.Opacity = 1; textTab3.TextColor = activeColor; }
        if (index == 4) { Tab4.IsVisible = true; iconTab4.Opacity = 1; textTab4.TextColor = activeColor; }
    }

    // --- LOGIC NGHIỆP VỤ ---
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Tab 3: Chức năng tra cứu biển số
    private async void OnTraCuuClicked(object sender, EventArgs e)
    {
        string plate = txtSearchPlate.Text?.Trim().ToUpper() ?? "";
        if (string.IsNullOrEmpty(plate))
        {
            await DisplayAlertAsync("Nhắc nhở", "Vui lòng nhập biển số xe cần kiểm tra!", "Đóng");
            return;
        }

        // Mô phỏng tìm thấy xe
        lblResPlate.Text = plate;
        cardResult.IsVisible = true;
        
        // Cuộn xuống để thấy kết quả (tùy chọn)
        txtSearchPlate.Unfocus(); // Tắt bàn phím
    }

    // Tab 3: Xác nhận xe ra
    private async void OnXacNhanRaClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Hoàn tất", $"Đã xác nhận xe {lblResPlate.Text} ra khỏi bãi.\nVé đã được tự động hủy!", "OK");
        
        // Reset form
        txtSearchPlate.Text = "";
        cardResult.IsVisible = false;
    }

    // Tab 4: Lưu cài đặt
    private async void OnLuuCaiDatClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Thành công", "Đã lưu cài đặt hệ thống vào máy chủ!", "Đóng");
    }
}