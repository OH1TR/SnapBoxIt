using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SnapBoxApp.Model;
using SnapBoxApp.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SnapBoxApp.ViewModels;

public class SearchViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private string _searchQuery = string.Empty;
    private bool _isLoading = false;
    private bool _hasResults = false;
    private bool _noResults = false;

    public SearchViewModel()
    {
        _apiService = new ApiService();
        SearchCommand = new Command<string>(async (query) => await PerformSearch(query));
        SearchResults = new ObservableCollection<SearchResultItem>();
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasResults
    {
        get => _hasResults;
        set => SetProperty(ref _hasResults, value);
    }

    public bool NoResults
    {
        get => _noResults;
        set => SetProperty(ref _noResults, value);
    }

    public ObservableCollection<SearchResultItem> SearchResults { get; }

    public ICommand SearchCommand { get; }

    private async Task PerformSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        try
        {
            IsLoading = true;
            HasResults = false;
            NoResults = false;
            SearchResults.Clear();

            var request = new SearchRequest
            {
                Query = query,
                Count = 10
            };

            var results = await _apiService.SearchItems(request);

            if (results.Any())
            {
                foreach (var item in results)
                {
                    var imageBytes = await _apiService.GetImageBytes(item.BlobId, true);
                    var searchResult = new SearchResultItem
                    {
                        Title = item.Title ?? "Ei otsikkoa",
                        Category = item.Category,
                        ImageBytes = imageBytes
                    };
                    SearchResults.Add(searchResult);
                }
                HasResults = true;
            }
            else
            {
                NoResults = true;
            }
        }
        catch (Exception)
        {
            // Handle error - could show alert to user
            NoResults = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public byte[]? ImageBytes { get; set; }
}
