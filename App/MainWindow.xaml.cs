using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.App.ViewModels;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Nrrdio.MapGenerator.App;

public sealed partial class MainWindow : Window {
    const int DEFAULT_WIDTH = 1800;
    const int DEFAULT_HEIGHT = 800;

    MainWindowViewModel ViewModel { get; }

    nint WindowHandle { get; }

    public MainWindow() {
        ViewModel = App.GetService<MainWindowViewModel>();
        Activated += ViewModel.Activate;

        InitializeComponent();

        WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        SetWindowSize(DEFAULT_WIDTH, DEFAULT_HEIGHT);
        CenterWindow();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel.Initialize(NavigationViewControl, NavigationFrame);
    }

    void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args) {
        AppTitleBar.Margin = new Thickness() {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    /// <summary>
    /// Source: https://github.com/microsoft/WinUI-3-Demos/blob/master/src/Build2020Demo/DemoBuildCs/DemoBuildCs/DemoBuildCs/App.xaml.cs#L28
    /// Updated to use https://github.com/Microsoft/CsWin32
    /// </summary>
    void SetWindowSize(int width, int height) {
        var windowHandle = new HWND(WindowHandle);

        var dpi = PInvoke.GetDpiForWindow(windowHandle);
        var scalingFactor = (float)dpi / 96;

        width = (int)(width * scalingFactor);
        height = (int)(height * scalingFactor);

        PInvoke.SetWindowPos(windowHandle, HWND.HWND_TOP, 0, 0, width, height, SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
    }

    /// <summary>
    /// Source: https://stackoverflow.com/a/71730765/2621693
    /// </summary>
    void CenterWindow() {
        var windowId = Win32Interop.GetWindowIdFromWindow(WindowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow is not null) {
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);

            if (displayArea is not null) {
                var CenteredPosition = appWindow.Position;
                CenteredPosition.X = ((displayArea.WorkArea.Width - appWindow.Size.Width) / 2);
                CenteredPosition.Y = ((displayArea.WorkArea.Height - appWindow.Size.Height) / 2);
                appWindow.Move(CenteredPosition);
            }
        }
    }
}
