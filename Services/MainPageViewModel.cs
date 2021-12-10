using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Nrrdio.Utilities.Loggers;
using System;
using System.Diagnostics;

namespace Nrrdio.MapGenerator.Services {
    public class MainPageViewModel {
        ILogger<MainPageViewModel> Log { get; }

        public MainPageViewModel() {
            Log = Ioc.Default.GetService<ILogger<MainPageViewModel>>();
            HandlerLoggerProvider.Instances[typeof(MainPageViewModel).FullName].EntryAddedEvent += MainPageViewModel_EntryAddedEvent;
            
            Log.LogInformation("test");
            Log.LogError("error!!");
            Log.LogWarning("warnung");
        }

        void MainPageViewModel_EntryAddedEvent(object sender, LogEntryEventArgs e) {
            Debug.WriteLine(e.LogEntry.Message);
        }
    }
}
