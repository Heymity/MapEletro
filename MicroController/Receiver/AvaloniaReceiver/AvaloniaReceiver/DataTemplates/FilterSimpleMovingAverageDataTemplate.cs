using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaReceiver.Filters;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterSimpleMovingAverageDataTemplate(string filterName, int averageSize, Action<int> removeBtn, Router.Router router)
    : FilterDataTemplate(filterName, removeBtn, router)
{
    public int AverageSize
    {
        get => Filter.AverageSize;
        set => Filter.AverageSize = value;
    }

    private SimpleMovingAverageFilter? _filter;
    private SimpleMovingAverageFilter Filter => _filter ??= new SimpleMovingAverageFilter(averageSize);
    
    [UsedImplicitly]
    public FilterSimpleMovingAverageDataTemplate(Router.Router router) : this("Moving Average", 10, _ => {}, router) { }

    public override IFilter GetFilter() => Filter;
    
    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Average Size",
                    BoundControl(new NumericUpDown { FormatString = "0", Increment = 1, Minimum = 1 },
                        NumericUpDown.ValueProperty, nameof(AverageSize))))
        ];
    }
}