using Nrrdio.MapGenerator.App.Contracts.Services;

namespace Nrrdio.MapGenerator.App.Services;

public class PageService : IPageService {
	public Dictionary<string, Type> Pages { get; } = new();

	public Type GetPageType(string key) {
		Type? pageType;

		lock (Pages) {
			if (!Pages.TryGetValue(key, out pageType)) {
				throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
			}
		}

		return pageType;
	}

	public void Configure(Type viewModel, Type page) {
		lock (Pages) {
			var viewModelName = viewModel.FullName!;

            Pages.TryAdd(viewModelName, page);

            //if (Pages.ContainsKey(viewModelName)) {
			//	throw new ArgumentException($"The key {viewModelName} is already configured in PageService");
			//}

			//if (Pages.Any(p => p.Value == page)) {
			//	throw new ArgumentException($"This type is already configured with key {Pages.First(p => p.Value == page).Key}");
			//}

			//Pages.Add(viewModelName, page);
		}
	}
}
