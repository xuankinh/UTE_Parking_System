using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace UTE_Parking_Project.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    // Hàm này chạy ngay khi màn hình vừa hiện lên
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Đợi 2.5 giây cho người dùng ngắm màn hình đẹp
        await Task.Delay(2500); 
        
        // Chuyển sang trang Login
        Application.Current!.MainPage = new NavigationPage(new LoginPage());
    }
}