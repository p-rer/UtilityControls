using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace UtilityControls;

public enum MemberIconType
{
    PropertyPublic,
    PropertyNonPublic,
    FieldPublic,
    FieldNonPublic,
    None
}

public class PropertyViewModel : INotifyPropertyChanged
{
    private readonly object? _object;

    private bool _isExpanded;

    public PropertyViewModel(string name, object? obj, MemberInfo? memberInfo = null, Type? type = null)
    {
        _object = obj;
        PropertyValue = obj?.ToString() ?? "null";
        PropertyName = name;
        switch (memberInfo)
        {
            case PropertyInfo pi:
                DisplayType = pi.PropertyType.Name;

                IconType = (pi.GetMethod != null && pi.GetMethod.IsPublic) ||
                           (pi.SetMethod != null && pi.SetMethod.IsPublic)
                    ? MemberIconType.PropertyPublic
                    : MemberIconType.PropertyNonPublic;
                break;
            case FieldInfo fi:
                DisplayType = fi.FieldType.Name;
                IconType = fi.IsPublic
                    ? MemberIconType.FieldPublic
                    : MemberIconType.FieldNonPublic;
                break;
            default:
                DisplayType = type?.Name ?? obj?.GetType().Name ?? "Unknown";
                IconType = MemberIconType.None;
                break;
        }

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

    public MemberIconType IconType { get; set; }

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

        var childItems = await Task.Run(async () =>
        {
            var items = new List<PropertyViewModel>();
            var propertyItems = new List<PropertyViewModel>();
            var fieldItems = new List<PropertyViewModel>();
            if (_object == null) return items;
            var type = _object.GetType();
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetMethod is { IsPrivate: false } || p.SetMethod is { IsPrivate: false })
                .ToList();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsPrivate)
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

                PropertyViewModel childVm;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    childVm = new PropertyViewModel(prop.Name, value, prop);
                    propertyItems.Add(childVm);
                });
            }

            propertyItems.Sort((a, b) =>
                string.Compare(a.PropertyName, b.PropertyName, StringComparison.OrdinalIgnoreCase));

            foreach (var field in fields)
            {
                object? value = null;
                try
                {
                    value = field.GetValue(_object);
                }
                catch
                {
                    // ignored
                }

                PropertyViewModel childVm;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    childVm = new PropertyViewModel(field.Name, value, field);
                    fieldItems.Add(childVm);
                });
            }

            fieldItems.Sort(
                (a, b) => string.Compare(a.PropertyName, b.PropertyName, StringComparison.OrdinalIgnoreCase));
            items.AddRange(propertyItems);
            items.AddRange(fieldItems);

            if (_object is IEnumerable enumerable and not string)
            {
                var indexerNode = new PropertyViewModel("this[]", _object);
                indexerNode.Children.Clear();
                var index = 0;
                foreach (var element in enumerable)
                {
                    var elementNode = new PropertyViewModel("[" + index + "]", element);
                    indexerNode.Children.Add(elementNode);
                    index++;
                }

                indexerNode.ChildrenLoaded = true;
                items.Add(indexerNode);
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