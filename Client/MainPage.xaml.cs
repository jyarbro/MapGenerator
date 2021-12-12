using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.Utilities.Loggers;
using System;

namespace Nrrdio.MapGenerator.Client {
    public sealed partial class MainPage : Page {
        ILogger<MainPageViewModel> Log { get; }
        MainPageViewModel ViewModel { get; }

        public MainPage() {
            InitializeComponent();
            
            Log = Ioc.Default.GetService<ILogger<MainPageViewModel>>();
            HandlerLoggerProvider.Instances[typeof(MainPageViewModel).FullName].EntryAddedEvent += MainPage_EntryAddedEvent;

            ViewModel = Ioc.Default.GetService<MainPageViewModel>();
            ViewModel.UpdateCanvas(OutputCanvas);

            DataContext = ViewModel;
        }

        void MainPage_EntryAddedEvent(object sender, LogEntryEventArgs e) {
            LogText.Text = $"{DateTime.Now:HH:mm:ss:ff}: {e.LogEntry.Message}\n{LogText.Text}";
        }

        void PageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Log.LogInformation(nameof(PageLoaded));
            ViewModel.GenerateAndDraw();
        }
    }
}
