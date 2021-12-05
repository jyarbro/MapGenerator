using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Nrrdio.Utilities.Loggers;

namespace Nrrdio.MapGenerator.Client {
    public partial class App : Application {
        public new static App Current => (App)Application.Current;

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
            mainWindow.Activate();
        }
    }
}
