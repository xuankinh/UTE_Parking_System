using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel; // Chứa lệnh gọi Launcher mở Web
using System;
using System.Threading.Tasks;
using UTE_Parking_Project.Services;

namespace UTE_Parking_Project.Views;

public partial class LoginPage : ContentPage
{
    private readonly FileHandlingService _fileService = new FileHandlingService();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        string user = txtUsername.Text?.Trim() ?? "";
        string pass = txtPassword.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetBtn("error", "✗ Vui lòng nhập đủ thông tin");
            await Task.Delay(1500);
            SetBtn("default", "Đăng nhập");
            return;
        }

        SetBtn("loading", "Đang xác thực...");
        await Task.Delay(700);

        LoginResult? result = _fileService.VerifyAccount(user, pass);

        if (result != null)
        {
            SetBtn("success", "✓ Thành công!");
            await Task.Delay(500);

            if (result.Role.Equals("Guard", StringComparison.OrdinalIgnoreCase))
                await Navigation.PushAsync(new GuardDashboard(result.FullName));
            else
                await Navigation.PushAsync(new StudentDashboard(result.FullName, result.Username, result.Email));
        }
        else
        {
            SetBtn("error", "✗ Sai tài khoản hoặc mật khẩu");
            await Task.Delay(1800);
            SetBtn("default", "Đăng nhập");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        txtUsername.Text = "";
        txtPassword.Text = "";
        SetBtn("default", "Đăng nhập");
    }

    // 🚀 HÀM MỚI: Click vào chữ CTSV bay thẳng ra trình duyệt web trường
    private async void OnLienHeWebClicked(object sender, EventArgs e)
    {
        // Sử dụng Launcher của MAUI để mở link trên trình duyệt mặc định
        await Launcher.OpenAsync("https://online.hcmute.edu.vn/");
    }

    private void SetBtn(string state, string text)
    {
        btnLogin.Text = text;
        switch (state)
        {
            case "loading":
                btnLogin.IsEnabled  = false;
                btnLogin.Opacity    = 0.75;
                btnLogin.Background = GradientBlue();
                break;
            case "success":
                btnLogin.IsEnabled  = false;
                btnLogin.Opacity    = 1;
                btnLogin.Background = new SolidColorBrush(Color.FromArgb("#16a34a")); 
                break;
            case "error":
                btnLogin.IsEnabled  = true;
                btnLogin.Opacity    = 1;
                btnLogin.Background = new SolidColorBrush(Color.FromArgb("#dc2626")); 
                break;
            default:
                btnLogin.IsEnabled  = true;
                btnLogin.Opacity    = 1;
                btnLogin.Background = GradientBlue();
                break;
        }
    }

    private static LinearGradientBrush GradientBlue() =>
        new LinearGradientBrush(
            new GradientStopCollection {
                new GradientStop(Color.FromArgb("#1a56b0"), 0.0f),
                new GradientStop(Color.FromArgb("#2563eb"), 1.0f)
            },
            new Point(0, 0), new Point(1, 1));
}