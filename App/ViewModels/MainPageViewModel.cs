using CommunityToolkit.Mvvm.ComponentModel;
using Nrrdio.MapGenerator.Services;

namespace Nrrdio.MapGenerator.App.ViewModels;

public class MainPageViewModel : ObservableRecipient {
    Orchestrator Orchestrator { get; }

    public MainPageViewModel(
        Orchestrator orchestrator
    ) {
        Orchestrator = orchestrator;
    }

    public async Task Start() => await Orchestrator.Start();
}