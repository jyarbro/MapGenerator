using System.ComponentModel;

namespace Nrrdio.MapGenerator.App.Contracts.Services;
public interface IStateManager {
	void UpdateSettings(object? sender, PropertyChangedEventArgs e);
}