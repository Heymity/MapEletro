using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaReceiver.Filters;
using AvaloniaReceiver.Router;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterSourceChannelDataTemplate(string channelName, Action<int> removeBtn, Router.Router router)
    : FilterDataTemplate(filterName: "SourceChannel", removeBtn, router, routeFrom: RouteFromOptions.Channel)
{
    public string ChannelName { get; set; } = channelName;

    public override Channel? RouteFromChannel
    {
        get => Router.GetChannelOfName(ChannelName);
        set => ChannelName = value?.ChannelName ?? "";
    }

    private SourceChannelFilter? _filter;

    [UsedImplicitly]
    public FilterSourceChannelDataTemplate(Router.Router router) : this("Raw 0", _ => {}, router) { }

    public override IFilter? GetFilter()
    {
        return _filter ??= new SourceChannelFilter(Router);
    }
    
    
    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Source Channel",
                    BoundControl(new TextBox(),
                        TextBox.TextProperty, nameof(ChannelName))))
        ];
    }
}