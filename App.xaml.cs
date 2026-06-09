using Microsoft.Maui.Controls;

namespace UTE_Parking_Project;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
       
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        
        var window = new Window(new NavigationPage(new Views.SplashPage()));

        
        window.Width = 400;
        window.Height = 750;
        
        return window;
    }
}