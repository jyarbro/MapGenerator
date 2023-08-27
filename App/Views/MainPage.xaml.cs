using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.MapGenerator.App.Helpers;
using Nrrdio.MapGenerator.App.ViewModels;
using Nrrdio.MapGenerator.Services;
using Nrrdio.Utilities.Loggers;

namespace Nrrdio.MapGenerator.App.Views;

[RegisterPage(typeof(MainPageViewModel), typeof(MainPage))]
public sealed partial class MainPage : Page {
    public MainPageViewModel ViewModel { get; }

    ILogger<MainPage> Log { get; }
    ICanvasWrapper Canvas { get; }

    int Resizing { get; set; }
    bool Drawing { get; set; }

    public MainPage() {
        ViewModel = App.GetService<MainPageViewModel>();
        Log = App.GetService<ILogger<MainPage>>();
        Canvas = App.GetService<ICanvasWrapper>();

        InitializeComponent();

        Canvas.Initialize(OutputCanvas);

        DataContext = ViewModel;

        // Register this last.
        foreach (var logger in HandlerLoggerProvider.Instances) {
            logger.Value.EntryAddedEvent += OnAppendLogText;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) { }

    async Task Redraw() {
        Log.LogTrace($"{nameof(Redraw)}");

        if (Drawing) {
            Log.LogTrace($"Already drawing");
            return;
        }

        Drawing = true;
        await ViewModel.Start();
        Drawing = false;
    }

    void OnAppendLogText(object? sender, LogEntryEventArgs e) {
        try {
            LogText.Text = $"{DateTime.Now:HH:mm:ss:ff}: {e.LogEntry.Message}\n{LogText.Text}";
        }
        catch (COMException) { }
    }

    async void OnPageLoaded(object sender, RoutedEventArgs e) {
        Log.LogTrace($"Event: {nameof(OnPageLoaded)}");

        try {
            await Redraw();
        }
        catch (COMException exception) when (exception.Message.Contains("The object has been closed.")) { }
        catch (COMException exception) when (exception.Message.Contains("Catastrophic failure")) { }
    }

    async void OnSizeChanged(object sender, SizeChangedEventArgs e) {
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

    async void OnRedrawButtonClick(object sender, RoutedEventArgs e) {
        Log.LogTrace($"Event: {nameof(OnRedrawButtonClick)}");

        try {
            await Redraw();
        }
        catch (COMException exception) when (exception.Message.Contains("The object has been closed.")) { }
    }

    void OnContinueButtonClick(object sender, RoutedEventArgs e) {
        Log.LogTrace($"Event: {nameof(OnContinueButtonClick)}");
        ViewModel.Continue();
    }
}
