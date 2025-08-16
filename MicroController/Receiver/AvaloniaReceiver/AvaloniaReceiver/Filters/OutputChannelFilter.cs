using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaReceiver.Router;

namespace AvaloniaReceiver.Filters;

public class OutputChannelFilter : IFilter<(double timestamp, double value)>
{
    private IEnumerable<(double timestamp, double value)>? _rawSource;
    
    public Channel? OutputChannel { get; set; }
    
    public void SetInputConnection(IEnumerable<(double timestamp, double value)> source)
    {
        _rawSource = source;
    }

    public IEnumerable<(double timestamp, double value)> GetOutputConnection()
    {
        //Console.WriteLine("Output To channel");
        if (OutputChannel?.ChannelData is null || _rawSource is null) return [];
        //Console.WriteLine($"Output To channel2 {_rawSource.Count()}");
        OutputChannel.ChannelData.LastReceivedData.Clear();
        OutputChannel.ChannelData.LastReceivedData.AddRange(_rawSource);
        return _rawSource;
    }

    public override string ToString()
    {
        return $"OutputChannelFilter {(_rawSource is null ? "null Source" : _rawSource.Count().ToString())}";
    }
}