using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaReceiver.Filters;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterDecimateDataTemplate(string filterName, int decimateFactor, Action<int> removeBtn, Router.Router router) : FilterDataTemplate(filterName, removeBtn, router)
{
    public int DecimateFactor
    {
        get => DecimateFilter.DecimationFactor;
        set => DecimateFilter.DecimationFactor = value;
    } 

    [UsedImplicitly]
    public FilterDecimateDataTemplate(Router.Router r) : this("DecimateFilter", 2, _ => {}, r) { }
    
    private DecimateFilter<(double, double)>? _decimateFilter = null;

    private DecimateFilter<(double, double)> DecimateFilter => _decimateFilter ??= new DecimateFilter<(double, double)>(decimateFactor);

    public override IFilter GetFilter()
    {
        return DecimateFilter;
    }

    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Decimate Factor",
                    BoundControl(new NumericUpDown { FormatString = "0", Increment = 1, Minimum = 1 },
                        NumericUpDown.ValueProperty, nameof(DecimateFactor))))
        ];
    }
}