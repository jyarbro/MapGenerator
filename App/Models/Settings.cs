using CommunityToolkit.Mvvm.ComponentModel;

namespace Nrrdio.MapGenerator.App.Models;

public class Settings : ObservableObject {
	public string Theme {
		get => _theme;
		set => SetProperty(ref _theme, value);
	}
	string _theme = string.Empty;
}
