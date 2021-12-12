using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.Utilities.Loggers;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Client {
    public sealed partial class MainPage : Page {
        ILogger<MainPage> Log { get; }
        MainPageViewModel ViewModel { get; }

        int Resizing { get; set; }
        bool Drawing { get; set; }

        public MainPage() {
            InitializeComponent();

            Log = Ioc.Default.GetService<ILogger<MainPage>>();

            foreach (var logger in HandlerLoggerProvider.Instances) {
                logger.Value.EntryAddedEvent += OnAppendLogText;
            }

            ViewModel = Ioc.Default.GetService<MainPageViewModel>();
            ViewModel.SetCanvas(OutputCanvas);

            DataContext = ViewModel;
        }

        async Task Redraw() {
            if (Drawing) {
                return;
            }

            Drawing = true;
            Log.LogInformation($"{nameof(Redraw)}");
            await ViewModel.Start();
            Drawing = false;
        }

        void OnAppendLogText(object sender, LogEntryEventArgs e) {
            try {
                LogText.Text = $"{DateTime.Now:HH:mm:ss:ff}: {e.LogEntry.Message}\n{LogText.Text}";
            }
            catch (COMException) { }
        }

        async void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Log.LogTrace($"Event: {nameof(OnPageLoaded)}");

            try {
                await Redraw();
            }
            catch (COMException exception) when (exception.Message.Contains("The object has been closed.")) { }
        }

        async void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) {
            Log.LogTrace($"Event: {nameof(OnSizeChanged)}");

            Resizing++;
            await Task.Delay(250);
            Resizing--;

            if (Resizing == 0) {
                try {
                    await Redraw();
                }
                catch (COMException exception) when (exception.Message.Contains("The object has been closed.")) { }
            }
        }

        async void OnRedrawButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Log.LogTrace($"Event: {nameof(OnRedrawButtonClick)}");

            try {
                await Redraw();
            }
            catch (COMException exception) when (exception.Message.Contains("The object has been closed.")) { }
        }

        void OnContinueButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Log.LogTrace($"Event: {nameof(OnContinueButtonClick)}");
            ViewModel.Continue();
        }
    }
}
