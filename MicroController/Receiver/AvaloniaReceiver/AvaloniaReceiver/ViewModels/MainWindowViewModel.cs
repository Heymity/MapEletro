using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaReceiver.CustomControls;
using AvaloniaReceiver.DataTemplates;
using AvaloniaReceiver.Filters;
using AvaloniaReceiver.Router;
using AvaloniaReceiver.Serial;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Avalonia;
using OxyPlot.Axes;
using OxyPlot.Series;
using LineAnnotation = OxyPlot.Annotations.LineAnnotation;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using ScatterSeries = OxyPlot.Series.ScatterSeries;
using Series = OxyPlot.Series.Series;
using TimeSpanAxis = OxyPlot.Axes.TimeSpanAxis;

namespace AvaloniaReceiver.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const int WriteBufferSize = 128 * 1024;
    private const byte SeparatorByte = (byte)',';
    private const byte NewLineByte = (byte)'\n';
    
    // UI Bindings
    public PlotModel PlotModelTime { get; private set;  } = null!;
    public PlotModel PlotModelXy { get; private set; } = null!;
    public PlotModel PlotModelFFT { get; private set; } = null!;
    
    public bool IsConnected => _dataComPort?.Connected ?? false;
    public string ConnectBtnHeader => IsConnected ? "Disconnect" : "Connect";

    public float TimeRange { get; set; } = 10;

    private bool IsRecording
    {
        get => _isRecording;
        set
        {
            _isRecording = value;
            OnPropertyChanged(nameof(RecordingBtnHeader));
        }
    }

    public string RecordingBtnHeader => IsRecording ? "Save Recording" : "Start Recording";
    
    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set => SetProperty(ref _isPaneOpen, value);
    }

    public int XYDecimateFactor { get; set; }

    public ObservableCollection<ChannelListDataTemplate> ChannelsListBinding { get; set; } = [];
    public ObservableCollection<FilterDataTemplate> FilterListBoxItems { get; set; } = [];
    public string[] AvailableFilters { get; private init; }
    public int XyPointCount { get; set; } = 500;

    public int GridLenX { get; set; } = 20;
    public int GridLenY { get; set; } = 20;
    public double GridSeparationX { get; set; } = 5/20d;
    public double GridSeparationY { get; set; } = 5/20d;
    public double GridOffsetX { get; set; } = 0;
    public double GridOffsetY { get; set; } = 0;
  

    // Private variables
    private IStorageFile? _file;

    private int _bufferHead;
    private readonly byte[] _recordingBuffer = new byte[WriteBufferSize];
    private Stream? _recordingStream;
    
    private ComPort? _dataComPort;

    private int _xyPlotIndex;
    
    private bool _isRecording;
    private bool _isPaneOpen = true;

    private readonly Dictionary<string, Type> _filterMap;

    private FilterSeries<(double timestamp, double value), (double timestamp, double value)> _plotFilter = null!;

    private readonly Router.Router _router;

    private string _xChannelName = "";
    private string _yChannelName = "";

    private DecimateFilter<ScatterPoint> _xyDecimate = new DecimateFilter<ScatterPoint>(1);
    private ScatterSeries _markingsScatterSeries = new ScatterSeries { MarkerType = MarkerType.Diamond, MarkerSize = 5, MarkerFill = OxyColors.Magenta };
    private ScatterSeries _referenceScatterSeries = new ScatterSeries { MarkerType = MarkerType.Square, MarkerSize = 3, MarkerFill = OxyColors.Red };
    private ScatterSeries _scatterSeries = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerSize = 1d };
    private (double X, double Y) _currentPoint = (0, 0);
    
    public MainWindowViewModel()
    {
        SetupPlotModel();

        _router = Router.Router.Singleton;
        
        var filters = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(FilterDataTemplate).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ToList();

        _filterMap = filters.Select(t => (new string(t.Name.Replace("Filter", null).Replace("DataTemplate", null).TakeWhile(x => x != '`').ToArray()), t)).OrderBy(x => x.Item1).ToDictionary();
        AvailableFilters = _filterMap.Keys.ToArray();
        
        Console.WriteLine($"Available Filters: {AvailableFilters.Length}");
        
        OnPropertyChanged(nameof(AvailableFilters));
        
        SetupFilters();
    }

    [RelayCommand]
    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    
    #region Filters
    public void AddNewFilterFromName(string? filterName)
    {
        if (filterName is null) return;

        var obj = Activator.CreateInstance(_filterMap[filterName], _router);
        if (obj is not FilterDataTemplate filterDataTemplate)
        {
            Console.WriteLine("Filter data template is not of type FilterDataTemplate.");
            return; 
        }

        filterDataTemplate.SetRemoveBtn(RemoveFilter);
        FilterListBoxItems.Add(filterDataTemplate);
        
        FiltersListChanged();
    }
    private void SetupFilters()
    {
        //ChannelsListBinding.Add(new ChannelListDataTemplate { ChannelName = "aaaa", ChannelColor = Color.FromRgb(255, 0, 255)});
        
        FilterListBoxItems.Add(new FilterSourceChannelDataTemplate("Raw 0", RemoveFilter, _router));
        //FilterListBoxItems.Add(new FilterDecimateDataTemplate("Decimate", 20, RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterSimpleMovingAverageDataTemplate("SMA 0", 1000, RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterChannelOutputDataTemplate("Output MA 0", RemoveFilter, _router));
        
        FilterListBoxItems.Add(new FilterSourceChannelDataTemplate("Raw 1", RemoveFilter, _router));
        //FilterListBoxItems.Add(new FilterDecimateDataTemplate("Decimate", 20, RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterSimpleMovingAverageDataTemplate("SMA 1", 1000, RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterChannelOutputDataTemplate("Output MA 1", RemoveFilter, _router));
        
        FilterListBoxItems.Add(new FilterSourceChannelDataTemplate("Raw 0", RemoveFilter, _router));
        //FilterListBoxItems.Add(new FilterDecimateDataTemplate("Decimate", 20, RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterFFTDataTemplate("FFT", RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterFFTChannelOutput("Output FFT", RemoveFilter, _router));
        FilterListBoxItems.Add(new FilterSourceChannelDataTemplate("Raw 1", RemoveFilter, _router));
        
        BuildFilter();
        
        
        OnPropertyChanged(nameof(ChannelsListBinding));
        
        ChannelListDataTemplate.OnColorChanged += OnChannelColorChanged;
        ChannelListDataTemplate.OnChannelToggled += OnChannelToggled;

        FilterListBoxItem.ListChanged += FiltersListChanged;
    }
    private void RemoveFilter(int id)
    {
        int? index = null;
        for (var j = 0; j < FilterListBoxItems.Count; j++)
        {
            if (FilterListBoxItems[j].FilterInstanceId != id) continue;
            index = j;
            break;
        }
        if (index == null) return;
        FilterListBoxItems.RemoveAt(index.Value);

        FiltersListChanged();
    }
    private async void FiltersListChanged()
    {
        try
        {
            await Task.Run(BuildFilter);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await GetErrorBox("Error building the filter", ex.Message).ShowAsync();
        }

        OnPropertyChanged(nameof(FilterListBoxItems));
    }
    private void BuildFilter()
    {
        _router.Clear();
        foreach (var filterData in FilterListBoxItems)
        {
            _router.RouteFilter(filterData);
        }
        
        BuildChannels(true);
        Console.WriteLine(_router);
    }
    #endregion
    
    #region Recording
    [RelayCommand]
    public async Task RecordingBtnCommand()
    {
        if (IsRecording) await EndRecording();
        else await StartRecording();
    }
    
    private async Task EndRecording()
    {
        IsRecording = false;

        await WriteBuffer();

        if (_recordingStream != null)
        {
            _recordingStream.Close();
            await _recordingStream.DisposeAsync();
            _recordingStream = null;
        }
    }
    
    private async Task StartRecording()
    {
        _file = await DoSaveFilePickerAsync();
        if (_file is null) return;

        IsRecording = true;
            
        _recordingStream = await _file.OpenWriteAsync();
    }
    
    private async Task WriteBuffer()
    {
        if (_file is null || _recordingStream is null) throw new NullReferenceException("File is null");
        Console.WriteLine("Writing buffer");
        
        await _recordingStream.WriteAsync(_recordingBuffer.AsMemory(0, _bufferHead));
        
        _bufferHead = 0;
    }
    #endregion
    
    #region Channels
    private LineSeries? LineSeriesFromName(string channelName)
    {
        LineSeries? ls = null;
        var fftIndex = 0;
        for (var i = 0; i < ChannelsListBinding.Count; i++)
        {
            if (ChannelsListBinding[i].Channel is FFTChannel)
            {
                if (ChannelsListBinding[i].ChannelName == channelName)
                {
                    ls = PlotModelFFT.Series[fftIndex] as LineSeries;
                    break;
                }
                fftIndex++;
            } else if (ChannelsListBinding[i].ChannelName == channelName)
            {
                ls = PlotModelTime.Series[i - fftIndex] as LineSeries;
                break;
            }
        }

        return ls;
    }
    
    private void OnChannelColorChanged(string channelName, Color newColor)
    {
        var series = LineSeriesFromName(channelName);
        if (series is null) return;
        series.Color = newColor.ToOxyColor();
    }
    
    private void OnChannelToggled(string channelName, bool enable)
    {
        var series = LineSeriesFromName(channelName);
        if (series is null) return;
        series.IsVisible = enable;
    }
    
    private void BuildChannels(bool force = false)
    {
        //Console.WriteLine("Channels: " + string.Join("\n\r    > ", ChannelsListBinding.Select(x => x.Channel)));
        /*if (!force &&
            (ChannelsListBinding.Count(c => c.RawDataInput) == (_dataComPort?.NumChannels ?? 0) &&
            ChannelsListBinding.Count(c => !c.RawDataInput) == _router.GetNotRawChannelOutputCount()))
            return;*/
        
        PlotModelTime.Series.Clear();
        PlotModelFFT.Series.Clear();

        Console.WriteLine("Building new channels");
        
        var tmpChannelsList = new List<ChannelListDataTemplate>();
        if (_dataComPort is not null)
        {
            for (var i = 0; i < _dataComPort.NumChannels; i++)
            {
                LineSeries lineSeries;
                var rawChannelKey = $"Raw {i}";
                if (ChannelsListBinding.FirstOrDefault(x => x.ChannelName == rawChannelKey) is { } channel)
                    lineSeries = new LineSeries { LineStyle = LineStyle.Solid, Color = channel.ChannelColor.ToOxyColor(), Title = rawChannelKey, IsVisible = channel.Enabled };
                else
                {
                    var color = ColorProvider(rawChannelKey);
                    lineSeries = new LineSeries { LineStyle = LineStyle.Solid, Color = color, Title = rawChannelKey };

                    channel = new ChannelListDataTemplate
                    {
                        ChannelName = lineSeries.Title, ChannelColor = color.ToColor(), Enabled = true,
                        RawDataInput = true
                    };
                }
                
                channel.Channel.SetInput(_dataComPort.DataSources[i]);
                tmpChannelsList.Add(channel);
                PlotModelTime.Series.Add(lineSeries);
            }
        }

        for (var i = 0; i < _router.GetNotRawChannelOutputCount(); i++)
        {
            LineSeries lineSeries;
            var chn = _router.GetNotRawChannelOfIndex(i);
            var channelKey = chn.ChannelName;
            Console.WriteLine(channelKey);
            if (ChannelsListBinding.FirstOrDefault(x => x.ChannelName == channelKey) is { } channel)
                lineSeries = new LineSeries { LineStyle = LineStyle.Solid, Color = channel.ChannelColor.ToOxyColor(), Title = channelKey, IsVisible = channel.Enabled };
            else
            {
                var color = ColorProvider(channelKey);
                lineSeries = new LineSeries { LineStyle = LineStyle.Solid, Color = color, Title = channelKey };
                    
                channel = new ChannelListDataTemplate
                {
                    Channel = chn, ChannelName = lineSeries.Title, ChannelColor = color.ToColor(), Enabled = true, RawDataInput = false
                };
            }
                
            tmpChannelsList.Add(channel);

            if (chn is FFTChannel fftChannel)
            {
                channel.ChannelColor = Colors.Red;
                PlotModelFFT.Series.Add(lineSeries);
                lineSeries.Color = OxyColors.Red;
            }
            else PlotModelTime.Series.Add(lineSeries);
        }
    
        Console.WriteLine("New Channels: " + string.Join("\n\r    > ", tmpChannelsList.Select(x => x.ChannelName)));
        
        ChannelsListBinding.Clear();
        _router.ClearChannels();
        foreach (var c in tmpChannelsList)
        {
            _router.RegisterChannel(c.Channel);
            ChannelsListBinding.Add(c);
        }

        _router.ReassignInputChannels();
        Console.WriteLine(_router);
        OnPropertyChanged(nameof(ChannelsListBinding));
    }
    
    private static OxyColor ColorProvider(string channelName)
    {
        var hash = (channelName.GetHashCode() * 3450298253u) % int.MaxValue;
        return OxyColor.FromRgb((byte)((hash >> 24) & 0xff), (byte)((hash >> 8) & 0xff), (byte)(hash & 0xff));
    }
    #endregion
    
    #region Plot
    private void SetupPlotModel()
    {
        PlotModelTime = new PlotModel();
        
        PlotModelTime.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = -5, Maximum = 5, MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot, MinorStep = 100});
        PlotModelTime.Axes.Add(new TimeSpanAxis { Position = AxisPosition.Bottom });
        
        PlotModelTime.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, LineStyle = LineStyle.Solid, Y = 4096, Color = OxyColor.FromRgb(200, 80, 80)});
        
        
        PlotModelXy = new PlotModel();
        
        foreach (var position in new[] {AxisPosition.Left, /*AxisPosition.Right,*/ AxisPosition.Bottom, /*AxisPosition.Top*/})
            PlotModelXy.Axes.Add(new LinearAxis { Position = position, MinimumRange = 0, Minimum = -5, Maximum = 5, MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot});
        
        PlotModelXy.PlotType = PlotType.Cartesian;


        PlotModelFFT = new PlotModel();
        
        PlotModelFFT.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = -40, Maximum = 100});
        PlotModelFFT.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });
        
    }
    
    private void UpdatePlot()
    {
        lock (_router.SyncLock)
        {
            _router.MaxTimestamp = _dataComPort!.CurrentTimestamp;
            
            if (ChannelsListBinding.Count(c => c.RawDataInput) != (_dataComPort?.NumChannels ?? 0) ||
                ChannelsListBinding.Count(c => !c.RawDataInput) != _router.GetNotRawChannelOutputCount())
            {
                BuildChannels();
                BuildFilter();
            }

            if (_dataComPort == null) return;
            lock (PlotModelTime.SyncRoot)
            {
                foreach (var axis in PlotModelTime.Axes)
                {
                    axis.MajorGridlineStyle = LineStyle.Dash;
                }

                var maxTimestamp = 0d;
                for (var i = 0; i < PlotModelTime.Series.Count; i++)
                {
                    var plotModelSeries = (LineSeries)PlotModelTime.Series[i];

                    var data = _router.GetChannelOutput(ChannelsListBinding.Where(c => c.Channel is not FFTChannel)
                        .Skip(i).First().ChannelId); //.ToList();
                    //Console.WriteLine($"plot channel {i} - {data.Count()}");
                    plotModelSeries.Points.AddRange(data);

                    if (!(TimeRange > 0)) continue;
                    if (plotModelSeries.Points.Count <= 0) continue;
                    var tmp = plotModelSeries.Points.Select(x => x.X).Max();
                    maxTimestamp = Math.Max(maxTimestamp, tmp);

                }

                foreach (var t in PlotModelTime.Series)
                {
                    if (!(TimeRange > 0)) continue;
                    var plotModelSeries = (LineSeries)t;
                    
                    if (plotModelSeries.Points.Count <= 0) continue;
                    
                    if (!(maxTimestamp - plotModelSeries.Points.FirstOrDefault().X >=
                          TimeRange)) continue;

                    plotModelSeries.Points.RemoveRange(0,
                        plotModelSeries.Points.TakeWhile(x => maxTimestamp - x.X > TimeRange).Count());
                }
            }


            lock (PlotModelXy.SyncRoot)
            {
                UpdateXYPlot();
            }

            lock (PlotModelFFT.SyncRoot)
            {
                foreach (var axis in PlotModelFFT.Axes)
                {
                    axis.MajorGridlineStyle = LineStyle.Dash;
                    if (axis.Position == AxisPosition.Left)
                    {
                        axis.Maximum = double.NaN;
                    }
                }

                for (var i = 0; i < PlotModelFFT.Series.Count; i++)
                {
                    var plotModelSeries = (LineSeries)PlotModelFFT.Series[i];

                    var chn = ChannelsListBinding.Where(c => c.Channel is FFTChannel).Skip(i).First();
                    //Console.WriteLine(chn.Channel);
                    var data = _router.GetChannelOutput(chn.ChannelId).ToList();
                    //Console.WriteLine($"plot channel {chn.Channel} - {i} - {data.Count()}");
                    if (data.Count == 0) continue;

                    plotModelSeries.Points.Clear();
                    plotModelSeries.Points.AddRange(data);
                }
            }


            PlotModelTime.InvalidatePlot(true);
            PlotModelXy.InvalidatePlot(true);
            PlotModelFFT.InvalidatePlot(true);

            _router.ResetChannelOutputCache();
        }
    }

    private void UpdateXYPlot()
    {
        _xyDecimate.DecimationFactor = XYDecimateFactor;

        if (PlotModelXy.Series.Count != 3)
        {
            PlotModelXy.Series.Clear();
                
            PlotModelXy.Series.Add(_scatterSeries);
            PlotModelXy.Series.Add(_markingsScatterSeries);
            PlotModelXy.Series.Add(_referenceScatterSeries);
        }

        if (_scatterSeries.Points.Count != XyPointCount)
        {
            _scatterSeries.Points.Clear();
            _scatterSeries.Points.AddRange(Enumerable.Range(0, XyPointCount).Select(_ => new ScatterPoint(0, 0)));
        }

        var channelX = _router.GetChannelOfName(_xChannelName);
        if (channelX is null) return;

        var channelY = _router.GetChannelOfName(_yChannelName);
        if (channelY is null) return;

        var dataX = _router.GetChannelOutput(channelX.Id);
        var dataY = _router.GetChannelOutput(channelY.Id);

        _xyDecimate.SetInputConnection(dataX.Zip(dataY)
            .Select<(DataPoint First, DataPoint Second), (double x, double y)>(x =>
                (x.First.Y, x.Second.Y))
            .TakeLast(XyPointCount)
            .Select(x => new ScatterPoint(x.x, x.y)));
                    
        foreach (var point in _xyDecimate.GetOutputConnection())
        {
            _scatterSeries.Points[_xyPlotIndex++ % XyPointCount] = point;
            _currentPoint = (point.X, point.Y);     
        }
    }
    
    
    [RelayCommand]
    public void MarkPoint()
    {
        _markingsScatterSeries.Points.Add(new ScatterPoint(_currentPoint.X, _currentPoint.Y));
    }

    [RelayCommand]
    public void CreateReferenceGrid()
    {
        _referenceScatterSeries.Points.Clear();

        Console.WriteLine($"{GridLenX} - {GridLenY}");
        for (int x = -(int)Math.Floor(GridLenX / 2d); x < Math.Floor(GridLenX / 2d); x++)
        {
            var xPos = x * GridSeparationX + GridOffsetX;
            for (int y = -(int)Math.Floor(GridLenY / 2d); y < Math.Floor(GridLenY / 2d); y++)
            {
                var yPos = y * GridSeparationY + GridOffsetY;
                Console.WriteLine($"Grid {xPos} {yPos}");
                _referenceScatterSeries.Points.Add(new ScatterPoint(xPos, yPos));
            }
        }
    }
    #endregion
    
    #region COMConnection
    
    public void ConnectPort(string port)
    {
        try
        {
            _dataComPort = new ComPort(port, DataReadyCallback);
            
            Console.WriteLine("Open COM port");
        
            _dataComPort.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            GetErrorBox("Error opening serial port", ex.Message).ShowAsync();
        }

        OnPropertyChanged(nameof(ConnectBtnHeader));
    }
    private async void DataReadyCallback()
    {
        if (_dataComPort is null) return;
        
        UpdatePlot();
        
        if (!IsRecording) return;
        
        var bytes = new List<byte>(500);
        for (var i = 0; i < _dataComPort.ReceivedData.Max(x => x.Count); i++)
        {
            bytes.AddRange(Encoding.Default.GetBytes(i.ToString()));
            
            for (var j = 0; j < _dataComPort.NumChannels; j++)
            {
                bytes.Add(SeparatorByte);
                if (i < _dataComPort.ReceivedData[j].Count) bytes.AddRange(Encoding.Default.GetBytes(_dataComPort.ReceivedData[j][i].timestamp.ToString(CultureInfo.InvariantCulture)));
                bytes.Add(SeparatorByte);
                if (i < _dataComPort.ReceivedData[j].Count) bytes.AddRange(Encoding.Default.GetBytes(_dataComPort.ReceivedData[j][i].value.ToString(CultureInfo.InvariantCulture)));
            }
            bytes.Add(NewLineByte);

            if (bytes.Count + _bufferHead > WriteBufferSize) 
                await WriteBuffer();
            bytes.ToArray().AsSpan().CopyTo(_recordingBuffer.AsSpan()[_bufferHead..]);
            _bufferHead += bytes.Count;
            
            bytes.Clear();
        }
    }

    public void Disconnect()
    {
        if (_dataComPort == null) return;
        _dataComPort.Close();
        _dataComPort.Dispose();
        _dataComPort = null;
        
        OnPropertyChanged(nameof(ConnectBtnHeader));
    }

    #endregion
    
    private static IMsBox<string> GetErrorBox(string title, string message)
    {
        return MessageBoxManager.GetMessageBoxCustom(
            new MessageBoxCustomParams
            {
                Icon = Icon.Error,
                ContentTitle = title,
                ContentMessage = message,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ButtonDefinitions =
                [
                    new ButtonDefinition { Name = "Ok", IsDefault = true }
                ]
            });
    }

    public void ClearPoints()
    {
        Console.WriteLine("Clear Points");
        foreach (var series in PlotModelTime.Series)
        {
            if (series is LineSeries ls) ls.Points.Clear();
        }
    }

    public void TimeRange_ValueChanged()
    {
        
    }
    
    private async Task<IStorageFile?> DoSaveFilePickerAsync()
    {
        // For learning purposes, we opted to directly get the reference
        // for StorageProvider APIs here inside the ViewModel. 

        // For your real-world apps, you should follow the MVVM principles
        // by making service classes and locating them with DI/IoC.

        // See DepInject project for a sample of how to accomplish this.
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        return await provider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save Recording File"
        });
    }



    public void UpdateXyChannels(string xChn, string yChn)
    {
        _xChannelName = xChn;
        _yChannelName = yChn;
    }
}
