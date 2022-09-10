using System.Collections.ObjectModel;

using App.Contracts.ViewModels;
using App.Core.Contracts.Services;
using App.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace App.ViewModels;

public class MainViewModel : ObservableRecipient, INavigationAware {
    private readonly ISampleDataService _sampleDataService;
    private SampleOrder? _selected;

    public SampleOrder? Selected {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public ObservableCollection<SampleOrder> SampleItems { get; private set; } = new ObservableCollection<SampleOrder>();

    public MainViewModel(ISampleDataService sampleDataService) {
        _sampleDataService = sampleDataService;
    }

    public async void OnNavigatedTo(object parameter) {
        SampleItems.Clear();

        // TODO: Replace with real data.
        var data = await _sampleDataService.GetListDetailsDataAsync();

        foreach (var item in data) {
            SampleItems.Add(item);
        }
    }

    public void OnNavigatedFrom() {
    }

    public void EnsureItemSelected() {
        Selected ??= SampleItems.First();
    }
}
