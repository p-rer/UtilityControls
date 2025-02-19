using System.Collections.ObjectModel;
using System.Windows;

namespace UtilityControls;

public partial class PropertyTree
{
    internal static readonly DependencyProperty RootPropertiesProperty =
        DependencyProperty.Register(nameof(RootProperties), typeof(ObservableCollection<PropertyViewModel>),
            typeof(PropertyTree));

    public PropertyTree()
    {
        InitializeComponent();
        DataContext = this;
    }

    internal ObservableCollection<PropertyViewModel>? RootProperties
    {
        get => (ObservableCollection<PropertyViewModel>)GetValue(RootPropertiesProperty);
        set => SetValue(RootPropertiesProperty, value ?? DependencyProperty.UnsetValue);
    }

    public object? RootObject
    {
        set => RootProperties = value != null
            ? [new PropertyViewModel(value.ToString() ?? string.Empty, value, type: value.GetType())]
            : null;
    }
}