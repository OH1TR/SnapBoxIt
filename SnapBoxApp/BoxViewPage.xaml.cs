using SnapBoxApp.ViewModels;

namespace SnapBoxApp;

public partial class BoxViewPage : ContentPage
{
    public BoxViewPage()
    {
        InitializeComponent();
        BindingContext = new BoxViewModel();
    }
}
