using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UtilityControls.Test;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(MainWindow),
            new PropertyMetadata(Colors.White));

    private bool _isLoading = true;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += MainWindow_Loaded;
    }

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSlidersFromColor();
        var dpd = DependencyPropertyDescriptor.FromProperty(ColorPicker.SelectedColorProperty, typeof(ColorPicker));
        dpd.AddValueChanged(Picker, ColorPicker_SelectedColorChanged);
    }

    private void ColorPicker_SelectedColorChanged(object? sender, EventArgs e)
    {
        UpdateSlidersFromColor();
    }

    private void UpdateSlidersFromColor()
    {
        var c = Picker.SelectedColor;
        SliderR.Value = c.R;
        SliderG.Value = c.G;
        SliderB.Value = c.B;
        SliderA.Value = c.A;
        _isLoading = false;
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var r = (byte)SliderR.Value;
        var g = (byte)SliderG.Value;
        var b = (byte)SliderB.Value;
        var a = (byte)SliderA.Value;
        Picker.SelectedColor = Color.FromArgb(a, r, g, b);
    }
}

public class ColorToHexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color) return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        return "#FFFFFFFF";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}