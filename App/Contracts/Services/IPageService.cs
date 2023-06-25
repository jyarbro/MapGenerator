namespace Nrrdio.MapGenerator.App.Contracts.Services;

public interface IPageService {
	Dictionary<string, Type> Pages { get; }

	Type GetPageType(string key);
	void Configure(Type viewModel, Type page);
}
