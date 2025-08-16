using SnapBoxApp.ViewModels;

namespace SnapBoxApp;

public partial class SearchPage : ContentPage
{
    public SearchPage()
    {
        InitializeComponent();
        BindingContext = new SearchViewModel();
    }
}
