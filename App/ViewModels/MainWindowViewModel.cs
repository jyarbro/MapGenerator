using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.MapGenerator.App.Contracts.Services;
using Nrrdio.MapGenerator.App.Helpers;
using Nrrdio.Utilities.WinUI;

namespace Nrrdio.MapGenerator.App.ViewModels;

public class MainWindowViewModel : ObservableRecipient {
    public string Title => "AppDisplayName".GetLocalized();
    public IList<object>? MenuItems => NavigationView?.MenuItems;

    public object? Selected {
        get => _Selected;
        set => SetProperty(ref _Selected, value);
    }
    object? _Selected;

    NavigationView? NavigationView { get; set; }

    INavigationService NavigationService { get; }
    IPageService PageService { get; }

    public MainWindowViewModel(
        INavigationService navigationService,
        IPageService pageService
    ) {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;

        PageService = pageService;
    }

    public void Activate(object? sender, WindowActivatedEventArgs args) {
        var pageTypes =
            from type in Assembly.GetExecutingAssembly().GetTypes()
            where type.IsDefined(typeof(RegisterPageAttribute), false)
            select type;

        foreach (var pageType in pageTypes.ToList()) {
            var pageDetails = pageType.GetCustomAttributes(typeof(RegisterPageAttribute), false).Cast<RegisterPageAttribute>().First();

            PageService.Configure(pageDetails.ViewModel, pageDetails.Page);
        }

        NavigationService.NavigateTo(PageService.Pages.First().Key);
    }

    public void Initialize(NavigationView view, Frame frame) {
        NavigationView = view;
        NavigationView.ItemInvoked += OnItemInvoked;

        NavigationService.Frame = frame;
    }

    public void UnregisterEvents() {
        if (NavigationView is not null) {
            NavigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType) {
        if (NavigationView is not null) {
            return GetSelectedItem(NavigationView.MenuItems, pageType) ?? GetSelectedItem(NavigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType) {
        foreach (var item in menuItems.OfType<NavigationViewItem>()) {
            if (IsMenuItemForPageType(item, pageType)) {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);

            if (selectedChild is not null) {
                return selectedChild;
            }
        }

        return null;
    }

    bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType) {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey) {
            return PageService.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }

    void OnNavigated(object sender, NavigationEventArgs e) {
        var selectedItem = GetSelectedItem(e.SourcePageType);

        if (selectedItem is not null) {
            Selected = selectedItem;
        }
    }

    void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args) {
        var selectedItem = args.InvokedItemContainer as NavigationViewItem;

        if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey) {
            NavigationService.NavigateTo(pageKey);
        }
    }
}
