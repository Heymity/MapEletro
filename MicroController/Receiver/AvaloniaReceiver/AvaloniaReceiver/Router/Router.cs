using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvaloniaReceiver.DataTemplates;
using AvaloniaReceiver.Filters;
using AvaloniaReceiver.Serial;
using OxyPlot;

namespace AvaloniaReceiver.Router;

public class Router
{
    public readonly object SyncLock = new();
    
    private static Router? _instance;
    public static Router Singleton => _instance ??= new Router();
    
    private Queue<(int index, FilterDataTemplate dataTemplate)> _filterMissingInputChannel = [];
    private List<IFilter> _filters = [];
    private List<Channel> _channels = [];
    
    private Router() {}
    
    public double MaxTimestamp { get; set; }
    
    
    public void FeedSource(int channelId, List<(double, double)> data)
    {
        _filters.ForEach(f =>
        {
            if (f is not SourceChannelFilter filter) return;
            //Console.WriteLine($"Feeding {filter}");
            if (filter.SourceChannel?.Id == channelId)
            {
                //Console.WriteLine("feed " + channelId + " IN " + filter);
                filter.SetInputConnection(data);
            }
        });
    }
    
    private Dictionary<int, List<(double timestamp, double value)>> _cachedData = new Dictionary<int, List<(double timestamp, double value)>>();

    public IEnumerable<(double timestamp, double value)> GetChannelOutputDouble(int channelId)
    {
        //Console.WriteLine($"Get {channelId}");
        if (_cachedData.TryGetValue(channelId, out var cachedOutput))
            return cachedOutput;
        
        //Console.WriteLine("Not Cached");
        var result = _channels.FirstOrDefault(c => c.Id == channelId)?.GetOutput().ToList();
        if (result is null)
        {
            Console.WriteLine("Channel not Found");
            return [];
        }

        _cachedData.Add(channelId, result);
        //Console.WriteLine($"Got {result.Count}");
        FeedSource(channelId, result);

        //Console.WriteLine($"Got 2 {result.Count}");
        return _cachedData[channelId];
    }
    public IEnumerable<DataPoint> GetChannelOutput(int channelId) => GetChannelOutputDouble(channelId).Select(x => new DataPoint(x.timestamp, x.value));

    public void ResetChannelOutputCache()
    {
        _cachedData.Clear();
    }
    
    public int GetNotRawChannelOutputCount()
    {
        return _channels.Count(c => !c.IsRaw);
    }

    public Channel? GetChannelOfName(string name)
    {
        //Console.WriteLine($"Looking Channel {name} OUT");
        //Console.WriteLine($"Channel names : {string.Join(",", _channels.Select(c => c.ChannelName))}");
        return _channels.FirstOrDefault(c => c.ChannelName.Equals(name));
    }
    
    public void RouteFilter(FilterDataTemplate filterData)
    {
        IFilter? filter;
        switch (filterData.RouteFrom)
        {
            case RouteFromOptions.Channel:
                filter = filterData.GetFilter();
                if (filter == null)
                {
                    Console.WriteLine("Filter is null");
                    return;
                }
                
                var fromChannel = filterData.RouteFromChannel;
                if (fromChannel == null)
                {
                    Console.WriteLine("No channel found");
                    
                    _filterMissingInputChannel.Enqueue((_filters.Count, filterData));
                    _filters.Add(filter);
                    return;
                }
                
                //filter.SetInputConnection(fromChannel.GetOutput());
                if (filter is SourceChannelFilter sourceFilter) sourceFilter.SourceChannel = fromChannel;
                
                _filters.Add(filter);
                break;
            default:
            case RouteFromOptions.PreviousFilter:
                var previousFilter = _filters.LastOrDefault();
                if (previousFilter == null)
                {
                    Console.WriteLine("No previous filter found");
                    return;
                }
                
                filter = filterData.GetFilter();
                if (filter == null)
                {
                    Console.WriteLine("Filter is null");
                    return;
                }

                if (previousFilter is IFilterSeries series)
                {
                    var seriesTypes = series.GetGenericTypes();
                    var filterTypes = filter.GetGenericTypes();
                    if (seriesTypes.TOut != filterTypes.TOut)
                    {
                        _filters.Remove(previousFilter);
                        
                        var newSeries = CreateFilterSeries(seriesTypes.TIn, filterTypes.TOut);
                        foreach (var f in series.GetFilters())
                        {
                            newSeries.AddFilter(f);
                        }

                        series = newSeries;
                        _filters.Add(newSeries);
                    }

                    series.AddFilter(filter);
                }
                else
                {
                    _filters.Remove(previousFilter);

                    var newSeries = CreateFilterSeries(previousFilter.GetGenericTypes().TIn, filter.GetGenericTypes().TOut);
                    
                    newSeries.AddFilter(previousFilter);
                    newSeries.AddFilter(filter);
                    
                    _filters.Add(newSeries);
                }
                
                break;
            
        }

        switch (filterData.RouteTo)
        {
            case RouteToOptions.Channel:
                filter = _filters.Last();

                if (filter is IFilterSeries series)
                    filter = series.GetFilters().Last();
                
                if (filter is OutputChannelFilter outputChannelFilter)
                {
                    Console.WriteLine($"FILTER IS OUTPUT");
                    outputChannelFilter.OutputChannel = filterData.RouteToChannel;
                    outputChannelFilter.OutputChannel.SetInput(new DataSource());
                    outputChannelFilter.OutputChannel.SetRequester(() => outputChannelFilter.GetOutputConnection());
                    //RegisterChannel(outputChannelFilter.OutputChannel);
                }

                //filterData.RouteToChannel.SetInput(() => filter.GetOutputConnection().Cast<(double, double)>());
                RegisterChannel(filterData.RouteToChannel);
                break;
            case RouteToOptions.FFTPlot:
                filter = _filters.Last();

                if (filter is IFilterSeries s)
                    filter = s.GetFilters().Last();
                
                if (filter is OutputFFTChannelFilter outputFFTChannelFilter && filterData is FilterFFTChannelOutput fftChannelOutput)
                {
                    Console.WriteLine($"FILTER IS ----> FFT <--- OUTPUT");
                    outputFFTChannelFilter.OutputChannel = fftChannelOutput.RouteToChannel;
                    outputFFTChannelFilter.OutputChannel.SetRequester(() => outputFFTChannelFilter.GetOutputConnection());
                    //RegisterChannel(outputChannelFilter.OutputChannel);
                }


                //filterData.RouteToChannel.SetInput(() => filter.GetOutputConnection().Cast<(double, double)>());
                RegisterChannel(filterData.RouteToChannel);
                break;
            case RouteToOptions.NextFilter:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return;


        IFilterSeries CreateFilterSeries(Type tIn, Type tOut)
        {
            var filterSeriesType = typeof(FilterSeries<,>).MakeGenericType(tIn, tOut);
                    
            if (filterSeriesType is null) throw new Exception("Couldn't create filter Series type");
                    
            var filterSeries = (IFilterSeries?)Activator.CreateInstance(filterSeriesType);
            if (filterSeries is null) throw new Exception("Couldn't instantiate filter Series");
            
            return filterSeries;
        }
        
         
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Router:");
        builder.AppendLine("  Channels (" + _channels.Count + " channels):");
        foreach (var channel in _channels)
            builder.AppendLine("    " + channel);
        builder.AppendLine("  Filters (" + _filters.Count + " filters):");
        foreach( var filter in _filters)
            builder.AppendLine("    " + filter);
        
        return builder.ToString();
    }

    public void Clear()
    {
        ClearFilters();
        ClearChannels();
    }

    public void ClearFilters()
    {
        _filters.Clear();
    }
    
    public void ClearChannels()
    {
        _channels.Clear();
    }
    
    public void RegisterChannel(Channel c)
    {
        _channels.Add(c);
    }

    public void ReassignInputChannels()
    {
        Queue<(int index, FilterDataTemplate dataTemplate)> stillMissing = [];
        while (_filterMissingInputChannel.TryDequeue(out var f))
        {
            var fromChannel = f.dataTemplate.RouteFromChannel;
            if (fromChannel == null)
            {
                Console.WriteLine("No channel found (again)");
                    
                stillMissing.Enqueue(f);
                return;
            }
            
            Console.WriteLine($"Successfully bound channel input in {f.index} - {_filters[f.index]} - {f.dataTemplate.GetFilter()}");
                
            _filters[f.index].SetInputConnection(fromChannel.GetOutput());
            if (f.dataTemplate.GetFilter() is SourceChannelFilter sourceFilter) sourceFilter.SourceChannel = fromChannel;
        }
        
        while (stillMissing.TryDequeue(out var f)) _filterMissingInputChannel.Enqueue(f);
    }

    public Channel GetNotRawChannelOfIndex(int i)
    {
        return _channels.Where(c => !c.IsRaw).Skip(i).First();
    }

    public bool IsCached(int channelId)
    {
        //Console.WriteLine($"Is Cached {channelId}? - {_cachedData.ContainsKey(channelId)}");
        return _cachedData.ContainsKey(channelId);
    }
}