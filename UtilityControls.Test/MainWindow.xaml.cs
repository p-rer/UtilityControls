namespace UtilityControls.Test;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        PropertyTree.RootObject = this;
    }
}