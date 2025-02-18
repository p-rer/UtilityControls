using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace UtilityControls;

public class PropertyViewModel : INotifyPropertyChanged
{
    private readonly object? _object;

    private bool _isExpanded;

    public PropertyViewModel(string name, object? obj, PropertyInfo? propInfo = null, Type? type = null)
    {
        _object = obj;
        PropertyName = name;
        PropertyValue = obj?.ToString() ?? "null";
        if (propInfo != null)
            DisplayType = propInfo.PropertyType.Name;
        else
            DisplayType = type == null ? "" : type.Name;

        IsLoading = false;
        CanExpand = HasExpandableProperties(obj);
        if (CanExpand) Children.Add(null);
    }

    public string PropertyName { get; set; }
    public string PropertyValue { get; set; }
    public string DisplayType { get; set; }

    public bool IsLoading { get; set; }

    public ObservableCollection<PropertyViewModel?> Children { get; set; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
            if (_isExpanded && CanExpand && !ChildrenLoaded) _ = LoadChildrenAsync();
        }
    }

    public bool ChildrenLoaded { get; private set; }

    public bool CanExpand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static bool HasExpandableProperties(object? obj)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetMethod != null && !p.GetMethod.IsPrivate)
            .ToList();
        return props.Count != 0;
    }

    public async Task LoadChildrenAsync()
    {
        if (ChildrenLoaded) return;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (Children.Count == 1)
            {
                if (Children[0] == null)
                {
                    var loadingDummy = new PropertyViewModel("Loading...", null)
                    {
                        IsLoading = true,
                        PropertyName = "Loading...",
                        PropertyValue = "",
                        DisplayType = ""
                    };
                    Children[0] = loadingDummy;
                }
                else
                {
                    Children[0]!.PropertyName = "Loading...";
                    Children[0]!.PropertyValue = "";
                    Children[0]!.DisplayType = "";
                    Children[0]!.IsLoading = true;
                }
            }
        });

        await Task.Yield();

        var childItems = await Task.Run(() =>
        {
            var items = new List<PropertyViewModel>();
            if (_object != null)
            {
                var type = _object.GetType();
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.GetMethod != null && !p.GetMethod.IsPrivate)
                    .ToList();
                foreach (var prop in props)
                {
                    object? value = null;
                    try
                    {
                        value = prop.GetValue(_object);
                    }
                    catch
                    {
                        // ignored
                    }

                    var childVm = new PropertyViewModel(prop.Name, value, prop);
                    items.Add(childVm);
                }
            }

            return items;
        });

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Children.Clear();
            foreach (var child in childItems)
                Children.Add(child);
        });

        ChildrenLoaded = true;
    }

    private void OnPropertyChanged(string propName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}