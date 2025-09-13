using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnapBoxApp;

public partial class SearchResultsView : ContentView
{
    public static readonly BindableProperty SearchResultsProperty =
        BindableProperty.Create(nameof(SearchResults), typeof(ObservableCollection<SearchResultItem>), typeof(SearchResultsView), 
            new ObservableCollection<SearchResultItem>(), propertyChanged: OnSearchResultsChanged);

    public static readonly BindableProperty HasResultsProperty =
        BindableProperty.Create(nameof(HasResults), typeof(bool), typeof(SearchResultsView), false);

    public static readonly BindableProperty NoResultsProperty =
        BindableProperty.Create(nameof(NoResults), typeof(bool), typeof(SearchResultsView), false);

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(SearchResultsView), false);

    public SearchResultsView()
    {
        InitializeComponent();
    }

    public ObservableCollection<SearchResultItem> SearchResults
    {
        get => (ObservableCollection<SearchResultItem>)GetValue(SearchResultsProperty);
        set => SetValue(SearchResultsProperty, value);
    }

    public bool HasResults
    {
        get => (bool)GetValue(HasResultsProperty);
        set => SetValue(HasResultsProperty, value);
    }

    public bool NoResults
    {
        get => (bool)GetValue(NoResultsProperty);
        set => SetValue(NoResultsProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private static void OnSearchResultsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SearchResultsView view && newValue is ObservableCollection<SearchResultItem> results)
        {
            // Update HasResults and NoResults based on the collection
            view.HasResults = results.Count > 0;
            view.NoResults = results.Count == 0 && !view.IsLoading;
        }
    }
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public byte[]? ImageBytes { get; set; }
    public string BoxId { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
}
