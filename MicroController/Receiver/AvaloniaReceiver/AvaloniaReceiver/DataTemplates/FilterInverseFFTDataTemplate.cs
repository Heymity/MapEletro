using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaReceiver.Filters;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterInverseFFTDataTemplate(string filterName, Action<int> rmBtn, Router.Router router) : FilterDataTemplate(filterName, rmBtn, router)
{
    public double SampleRate
    {
        get => Filter.SampleRate;
        set => Filter.SampleRate = value;
    }

    private InverseFFTFilter Filter => _filter ??= new InverseFFTFilter();

    [UsedImplicitly]
    public FilterInverseFFTDataTemplate(Router.Router router) : this("FFT Filter", _ => { }, router) { }

    private InverseFFTFilter? _filter;

    public override IFilter GetFilter() => Filter;

    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Sample Rate", 
                    BoundControl(new NumericUpDown(), NumericUpDown.ValueProperty, nameof(SampleRate))))
        ];
    }
}