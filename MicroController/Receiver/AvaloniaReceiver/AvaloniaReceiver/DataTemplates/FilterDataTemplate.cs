using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvaloniaReceiver.Filters;
using AvaloniaReceiver.Router;

namespace AvaloniaReceiver.DataTemplates;

public enum RouteFromOptions
{
    Channel,
    PreviousFilter
}
public enum RouteToOptions
{
    Channel,
    // ReSharper disable once InconsistentNaming
    FFTPlot,
    NextFilter,
}

public abstract class FilterDataTemplate
{
    protected readonly Router.Router Router;
    private static int _lastId; 
    protected static int NextId => _lastId + 1;
    public int FilterInstanceId { get; }
    public string FilterName { get; private set; }
    
    private Action<int> _removeBtn;

    public virtual Channel? RouteFromChannel { get; set; }
    
    protected Channel? _routeToChannel;
    public virtual Channel RouteToChannel => _routeToChannel ??= new Channel() { ChannelName = $"{FilterName}_{FilterInstanceId}", IsRaw = false, };
    
    public RouteFromOptions RouteFrom { get; set; }
    public RouteToOptions RouteTo { get; set; }
    
    protected FilterDataTemplate(string filterName, Action<int> removeBtn, Router.Router router, RouteFromOptions routeFrom = RouteFromOptions.PreviousFilter, RouteToOptions routeTo = RouteToOptions.NextFilter)
    {
        FilterName = filterName;
        RouteFrom = routeFrom;
        RouteTo = routeTo;
        this._removeBtn = removeBtn;
        Router = router;
        FilterInstanceId = _lastId++;
    }

    protected FilterDataTemplate(Router.Router router) : this("", _ => { }, router) { }

    public void SetRemoveBtn(Action<int> rBtn)
    {
        this._removeBtn = rBtn;
    }
    
    public abstract IFilter? GetFilter();

    public virtual List<Control> GetFilterParamsControls()
    {
        var panel = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(4, GridUnitType.Star),new ColumnDefinition(1, GridUnitType.Star),new ColumnDefinition(4, GridUnitType.Star)],
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        panel.Children.Add(BoundControl(new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left }, TextBlock.TextProperty, nameof(FilterName)));
        panel.Children.Add(new TextBlock {VerticalAlignment = VerticalAlignment.Center, Text = $"({FilterInstanceId})"});

        var btn = (Button)BoundControl(new Button
        {
            Content = "X",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        }, TextBlock.TextProperty, nameof(FilterName));
        btn.Click += RemoveSelf;
        panel.Children.Add(btn);
        
        Grid.SetColumn(panel.Children[1], 1);
        Grid.SetColumn(panel.Children[2], 2);
        
        return [panel];
    }

    private void RemoveSelf(object? sender, RoutedEventArgs e)
    {
        _removeBtn(FilterInstanceId);
    }

    protected static Control BoundControl(Control c, AvaloniaProperty prop, string paramName, BindingMode mode = BindingMode.TwoWay)
    {
        var binding = new Binding
        {
            Mode = BindingMode.TwoWay,
            Path = paramName,
        };
        
        c.Bind(prop, binding);
        return c;
    }
    
    protected static StackPanel GetParamsGrid(params (string label, Control control)[] labelControlPairs)
    {
        var stack = new StackPanel();
        stack.Children.AddRange(labelControlPairs
            .Select((lc, i) =>
            {
                var l = new Label { Content = lc.label, Target = lc.control, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(l, 0);
                //Grid.SetRow(l, i);
                Grid.SetColumn(lc.control, 1);
                //Grid.SetRow(lc.control, i);
                return (l, lc.control);
            })
            .Aggregate(new List<Control>(), (acc, cur) =>
            {
                var grid = new Grid
                {
                    Margin = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    ColumnDefinitions =
                        [new ColumnDefinition(1.2, GridUnitType.Star), new ColumnDefinition(1, GridUnitType.Star)]
                };
                grid.Children.Add(cur.l);
                grid.Children.Add(cur.control);
                acc.Add(grid);
                
                return acc;
            }));

        return stack;
    }
}