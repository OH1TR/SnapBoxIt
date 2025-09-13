using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SnapBoxApp.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SnapBoxApp.ViewModels;

public class BoxViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private ObservableCollection<string> _boxes = new();
    private string _selectedBox = string.Empty;
    private bool _isLoading = false;
    private bool _hasResults = false;
    private bool _noResults = false;

    public BoxViewModel()
    {
        _apiService = new ApiService();
        SearchResults = new ObservableCollection<SearchResultItem>();
        LoadBoxesCommand = new Command(async () => await LoadBoxes());
        LoadBoxContentsCommand = new Command<string>(async (boxId) => await LoadBoxContents(boxId));
        
        // Load boxes when view model is created
        Task.Run(async () => await LoadBoxes());
    }

    public ObservableCollection<string> Boxes
    {
        get => _boxes;
        set => SetProperty(ref _boxes, value);
    }

    public string SelectedBox
    {
        get => _selectedBox;
        set
        {
            if (SetProperty(ref _selectedBox, value) && !string.IsNullOrEmpty(value))
            {
                LoadBoxContentsCommand.Execute(value);
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsNotLoading => !IsLoading;

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

    public ICommand LoadBoxesCommand { get; }
    public ICommand LoadBoxContentsCommand { get; }

    private async Task LoadBoxes()
    {
        try
        {
            IsLoading = true;
            var boxes = await _apiService.GetBoxes();
            
            Device.BeginInvokeOnMainThread(() =>
            {
                Boxes.Clear();
                foreach (var box in boxes)
                {
                    Boxes.Add(box);
                }
            });
        }
        catch (Exception)
        {
            // Handle error - could show alert to user
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadBoxContents(string boxId)
    {
        if (string.IsNullOrWhiteSpace(boxId))
            return;

        try
        {
            IsLoading = true;
            HasResults = false;
            NoResults = false;
            SearchResults.Clear();

            var items = await _apiService.GetBoxContents(boxId);

            if (items.Any())
            {
                foreach (var item in items)
                {
                    var imageBytes = await _apiService.GetImageBytes(item.BlobId, true);
                    var searchResult = new SearchResultItem
                    {
                        Title = item.Title ?? "Ei otsikkoa",
                        Category = item.Category,
                        ImageBytes = imageBytes,
                        BoxId = item.BoxId,
                        DetailedDescription = item.DetailedDescription ?? "Ei kuvausta"
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
