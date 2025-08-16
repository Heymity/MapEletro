using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaReceiver.Filters;
using AvaloniaReceiver.Router;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterFFTChannelOutput : FilterDataTemplate
{
    public string ChannelName
    {
        get => RouteToChannel.ChannelName;
        set => RouteToChannel.ChannelName = value;
    }

    public override FFTChannel RouteToChannel => (FFTChannel)(_routeToChannel ??= new FFTChannel { ChannelName = $"{FilterName}_{FilterInstanceId}", IsRaw = false, });

    private OutputFFTChannelFilter? _filter;

    [UsedImplicitly]
    public FilterFFTChannelOutput(Router.Router router) : this($"Output FFT Channel {NextId}", _ => {}, router) { }

    public FilterFFTChannelOutput(string channelName, Action<int> removeBtn, Router.Router router) : base(filterName: "Output Channel", removeBtn, router, routeTo: RouteToOptions.FFTPlot)
    {
        _routeToChannel = new FFTChannel { ChannelName = channelName, IsRaw = false};
        ChannelName = channelName;
    }

    public override IFilter? GetFilter()
    {
        return _filter ??= new OutputFFTChannelFilter();
    }
    
    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Output FFT Channel",
                    BoundControl(new TextBox(),
                        TextBox.TextProperty, nameof(ChannelName))))
        ];
    }
}