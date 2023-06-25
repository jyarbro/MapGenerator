using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.MapGenerator.App.Helpers;
using Nrrdio.MapGenerator.App.ViewModels;

namespace Nrrdio.MapGenerator.App.Views;

[RegisterPage(typeof(MainPageViewModel), typeof(MainPage))]
public sealed partial class MainPage : Page {
    public MainPageViewModel ViewModel { get; }

    public MainPage() {
        ViewModel = App.GetService<MainPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) { }
}
