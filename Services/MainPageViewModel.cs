using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.DependencyInjection;

namespace Nrrdio.MapGenerator.Services {
    public class MainPageViewModel {
        ILogger<MainPageViewModel> Log { get; }

        public MainPageViewModel() {
            Log = Ioc.Default.GetService<ILogger<MainPageViewModel>>();
        }

        public void LogTest() {
            Log.LogInformation("test");
            Log.LogError("error!!");
            Log.LogWarning("warnung");
        }
    }
}
