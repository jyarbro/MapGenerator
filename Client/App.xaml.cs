using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.WinUI;
using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using WinRT;

namespace Nrrdio.MapGenerator.Client {
    public partial class App : Application {
        public new static App Current => (App)Application.Current;

        public IntPtr WindowHandle { get; private set; }

        Window mainWindow;

        public App() {
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    .AddSingleton<ILoggerProvider, HandlerLoggerProvider>()
                    .AddLogging()
                .BuildServiceProvider());

            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args) {
            mainWindow = new MainWindow();

            var windowNative = mainWindow.As<IWindowNative>();
            WindowHandle = windowNative.WindowHandle;

            mainWindow.Title = "Map Generator";
            mainWindow.Activate();

            SetWindowSize(WindowHandle, 800, 600);
        }

        // https://github.com/microsoft/WinUI-3-Demos/blob/420c48fe1613cb20b38000252369a0c556543eac/src/Build2020Demo/DemoBuildCs/DemoBuildCs/DemoBuildCs/App.xaml.cs#L41
        // The Window object doesn't have Width and Height properties in WInUI 3 Desktop yet.
        // To set the Width and Height, you can use the Win32 API SetWindowPos.
        // Note, you should apply the DPI scale factor if you are thinking of dpi instead of pixels.

        void SetWindowSize(IntPtr hwnd, int width, int height) {
            var dpi = PInvoke.User32.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            PInvoke.User32.SetWindowPos(hwnd, PInvoke.User32.SpecialWindowHandles.HWND_TOP,
                                        0, 0, width, height,
                                        PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE);
        }
    }
}
