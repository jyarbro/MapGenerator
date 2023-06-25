using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.MapGenerator.App.Contracts.Services;
using Nrrdio.MapGenerator.App.Contracts.ViewModels;
using Nrrdio.MapGenerator.App.Helpers;

namespace Nrrdio.MapGenerator.App.Services;

public class NavigationService : INavigationService {
	public event NavigatedEventHandler? Navigated;

	IPageService PageService { get; }

	object? lastParameterUsed;

	public Frame? Frame {
		get => _Frame;
		set {
			UnregisterFrameEvents();
			_Frame = value;
			RegisterFrameEvents();
		}
	}
	Frame? _Frame;

	public NavigationService(
		IPageService pageService
	) {
		PageService = pageService;
	}

	public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) {
		var pageType = PageService.GetPageType(pageKey);

		if (_Frame != null && (_Frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(lastParameterUsed)))) {
			_Frame.Tag = clearNavigation;
			
			var vmBeforeNavigation = _Frame.GetPageViewModel();
			var navigated = _Frame.Navigate(pageType, parameter);

			if (navigated) {
				lastParameterUsed = parameter;

				if (vmBeforeNavigation is INavigationAware navigationAware) {
					navigationAware.OnNavigatedFrom();
				}
			}

			return navigated;
		}

		return false;
	}

	void OnNavigated(object sender, NavigationEventArgs e) {
		if (sender is Frame frame) {
			if (frame.GetPageViewModel() is INavigationAware navigationAware) {
				navigationAware.OnNavigatedTo(e.Parameter);
			}

			Navigated?.Invoke(sender, e);
		}
	}

	void RegisterFrameEvents() {
		if (_Frame is not null) {
			_Frame.Navigated += OnNavigated;
		}
	}

	void UnregisterFrameEvents() {
		if (_Frame is not null) {
			_Frame.Navigated -= OnNavigated;
		}
	}
}
