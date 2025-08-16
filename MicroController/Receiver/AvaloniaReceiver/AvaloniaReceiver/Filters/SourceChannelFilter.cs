using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaReceiver.Router;

namespace AvaloniaReceiver.Filters;

public class SourceChannelFilter(Router.Router router) : IFilter<(double timestamp, double value), (double timestamp, double value)>
{
    private Router.Router _router = router;
    
    public Channel? SourceChannel { get; set; }
    
    public void SetInputConnection(IEnumerable<(double timestamp, double value)> source)
    {
    }

    public IEnumerable<(double timestamp, double value)> GetOutputConnection()
    {
        return SourceChannel is null
            ? Enumerable.Empty<(double timestamp, double value)>()
            : SourceChannel.GetOutput(!_router.IsCached(SourceChannel.Id)); //_router.GetChannelOutputDouble(SourceChannel.Id);
    }

    public override string ToString()
    {
        return $"SourceChannelFilter {(SourceChannel is null ? "null Source" : $"{SourceChannel.ChannelName} - {SourceChannel.Id}")}";
    }
}