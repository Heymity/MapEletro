using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaReceiver.Filters;
using AvaloniaReceiver.Router;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterChannelOutputDataTemplate : FilterDataTemplate
{
    public string ChannelName
    {
        get => RouteToChannel.ChannelName;
        set => RouteToChannel.ChannelName = value;
    }

    public override Channel RouteToChannel => _routeToChannel ??= new Channel() { ChannelName = $"{FilterName}_{FilterInstanceId}", IsRaw = false, };

    private OutputChannelFilter? _filter;

    [UsedImplicitly]
    public FilterChannelOutputDataTemplate(Router.Router router) : this($"Output Channel {NextId}", _ => {}, router) { }

    public FilterChannelOutputDataTemplate(string channelName, Action<int> removeBtn, Router.Router router) : base(filterName: "Output Channel", removeBtn, router, routeTo: RouteToOptions.Channel)
    {
        _routeToChannel = new Channel { ChannelName = channelName, IsRaw = false};
        ChannelName = channelName;
    }

    public override IFilter? GetFilter()
    {
        return _filter ??= new OutputChannelFilter();
    }
    
    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Output Channel",
                    BoundControl(new TextBox(),
                        TextBox.TextProperty, nameof(ChannelName))))
        ];
    }
}