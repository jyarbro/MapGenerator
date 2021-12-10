using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.Utilities.Loggers;

namespace Nrrdio.MapGenerator.Client {
    public sealed partial class MainPage : Page {
        public MainPage() => InitializeComponent();

        void MainPage_EntryAddedEvent(object sender, LogEntryEventArgs e) {
            Log.Text = $"{e.LogEntry.Message}\n{Log.Text}";
        }

        void PageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            HandlerLoggerProvider.Instances[typeof(MainPageViewModel).FullName].EntryAddedEvent += MainPage_EntryAddedEvent;

            var log = Ioc.Default.GetService<ILogger<MainPageViewModel>>();

            log.LogInformation("hahahhaha");
            log.LogInformation("hahahhaha");
            log.LogInformation("hahahhaha");
        }
    }
}
