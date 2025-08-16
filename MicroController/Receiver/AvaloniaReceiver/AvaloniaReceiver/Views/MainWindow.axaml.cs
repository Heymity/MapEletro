using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using AvaloniaReceiver.DataTemplates;
using AvaloniaReceiver.Serial;
using AvaloniaReceiver.ViewModels;
using OxyPlot.Avalonia;

namespace AvaloniaReceiver.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ComboBoxPorts_OnDropDownOpened(object? sender, EventArgs e)
    {
        ((ComboBox)sender!).ItemsSource = ComPort.ReadableListPorts;
    }

    private void ConnectMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var port = ComboBoxPorts.SelectedItem?.ToString();
        if (port is null) return;
        var viewModel = ((MainWindowViewModel)DataContext!);

        if (!viewModel.IsConnected)
            viewModel.ConnectPort(port);
        else
            viewModel.Disconnect();
    }

    private void ClearPointsMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).ClearPoints();
    }

    private void PlotXy_OnLayoutUpdated(object? sender, EventArgs e)
    {
        
    }

    private void TimeRange_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).TimeRange_ValueChanged();
    }

    private void RecordingBrn_Click(object? sender, RoutedEventArgs e)
    {
        
    }

    private void ColorView_OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (sender is not ColorView colorView) return;

        if (colorView.DataContext is not ChannelListDataTemplate channelData) return;
        
        ChannelListDataTemplate.OnColorChanged?.Invoke(channelData.ChannelName, channelData.ChannelColor);
    }

    private void ChannelEnable_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton radioButton) return;
        if (radioButton.DataContext is not ChannelListDataTemplate channelData) return;
        ChannelListDataTemplate.OnChannelToggled?.Invoke(channelData.ChannelName, radioButton.IsChecked ?? true);
    }

    private void AddNewFilter_OnClick(object? sender, RoutedEventArgs e)
    {
        if (NextFilterName.SelectedItem is null) return;
        if (DataContext is not MainWindowViewModel viewModel) return;
        viewModel.AddNewFilterFromName(NextFilterName.SelectedItem.ToString());
    }

    private void UpdateChannelListOnXyPlot(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        XChannelSelect.ItemsSource = viewModel.ChannelsListBinding.Select(c => c.ChannelName);
        YChannelSelect.ItemsSource = viewModel.ChannelsListBinding.Select(c => c.ChannelName);
    }

    private void UpdateXyChannels(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        if (XChannelSelect.SelectedItem is not string x) return;
        if (YChannelSelect.SelectedItem is not string y) return;
        viewModel.UpdateXyChannels(x, y);
    }
}