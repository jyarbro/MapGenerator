using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.Utilities.Loggers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Client {
    public sealed partial class MainPage : Page {
        ILogger<MainPageViewModel> Log { get; }
        MainPageViewModel ViewModel { get; }

        int Resizing { get; set; }
        bool Drawing { get; set; }

        public MainPage() {
            InitializeComponent();
            
            Log = Ioc.Default.GetService<ILogger<MainPageViewModel>>();
            HandlerLoggerProvider.Instances[typeof(MainPageViewModel).FullName].EntryAddedEvent += OnAppendLogText;

            ViewModel = Ioc.Default.GetService<MainPageViewModel>();
            ViewModel.SetCanvas(OutputCanvas);

            DataContext = ViewModel;
        }

        void Redraw() {
            if (Drawing) {
                return;
            }

            Drawing = true;

            Log.LogInformation($"{nameof(Redraw)}");

            OutputCanvas.Children.Clear();
            ViewModel.GenerateAndDraw();

            Drawing = false;
        }

        void OnAppendLogText(object sender, LogEntryEventArgs e) {
            LogText.Text = $"{DateTime.Now:HH:mm:ss:ff}: {e.LogEntry.Message}\n{LogText.Text}";
        }

        void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Log.LogInformation($"Event: {nameof(OnPageLoaded)}");
            Redraw();
        }

        void OnRedrawButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Log.LogInformation($"Event: {nameof(OnRedrawButtonClick)}");
            Redraw();
        }

        async void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) {
            Resizing++;

            await Task.Run(() => {
                Thread.Sleep(250);
                Resizing--;
            });

            if (Resizing == 0) {
                Redraw();
            }
        }
            }
}
