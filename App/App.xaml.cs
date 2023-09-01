using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nrrdio.MapGenerator.App.Contracts.Services;
using Nrrdio.MapGenerator.App.Models;
using Nrrdio.MapGenerator.App.Services;
using Nrrdio.MapGenerator.App.ViewModels;
using Nrrdio.MapGenerator.App.Views;
using Nrrdio.MapGenerator.Services;
using Nrrdio.Utilities;
using Nrrdio.Utilities.Loggers;

namespace Nrrdio.MapGenerator.App;

public partial class App : Application {
	public static Window MainWindow { get; } = new MainWindow();

    public IHost Host { get; }

    public App() {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration((context, builder) => {
                builder.Sources.Clear();
                StateManager.EnsureSettings();
                builder.AddJsonFile("localSettings.json");
                builder.AddJsonFile(StateManager.SettingsPath, optional: true);
                builder.AddEnvironmentVariables();
            })
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    public static T GetService<T>()
        where T : class {

        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service) {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        MainWindow.Activate();
        
        GetService<INavigationService>().NavigateTo(typeof(MainPageViewModel).FullName!, args.Arguments);
    }

    void ConfigureServices(HostBuilderContext context, IServiceCollection services) {
        services.AddSingleton<ILoggerProvider, HandlerLoggerProvider>(_ => new HandlerLoggerProvider { LogLevel = LogLevel.Trace });
        services.AddSingleton<Wait>();

        services.AddScoped<IStateManager, StateManager>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<ICanvasWrapper, CanvasWrapper>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<MainPage>();

        services.AddTransient<Orchestrator>();
        services.AddTransient<VoronoiTesselator>();

        services
            .AddOptions<Settings>()
            .Bind(context.Configuration.GetSection(nameof(Settings)))
            .ValidateDataAnnotations();

        services.AddLogging();
    }
}
