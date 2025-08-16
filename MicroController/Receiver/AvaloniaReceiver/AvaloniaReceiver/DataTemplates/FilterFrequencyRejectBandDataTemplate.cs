using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaReceiver.Filters;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

public class FilterFrequencyRejectBandDataTemplate(string filterName, Action<int> rmBtn, Router.Router router) : FilterDataTemplate(filterName, rmBtn, router)
{
    public uint WindowSizePowerOfTwo
    {
        get => Filter.WindowSizePowerOfTwo;
        set => Filter.WindowSizePowerOfTwo = value;
    }

    public double SampleRate
    {
        get => Filter.SampleRate;
        set => Filter.SampleRate = value;
    }

    public WindowEnum WindowType
    {
        get => Filter.WindowType;
        set => Filter.WindowType = value;
    }
    
    public int SelectedWindowIndex
    {
        get => (int)WindowType;
        set => WindowType = (WindowEnum)value;
    }
    
    [UsedImplicitly]
    public FilterFrequencyRejectBandDataTemplate(Router.Router router) : this("Reject band FFT", i => {}, router) { }

    private FrequencyRejectBand? _filter;
    private FrequencyRejectBand Filter => _filter ??= new FrequencyRejectBand();
    public override IFilter? GetFilter() => Filter;
    
    public override List<Control> GetFilterParamsControls()
    {
        return
        [
            ..base.GetFilterParamsControls(),
            GetParamsGrid(
                ("Sample Rate", 
                    BoundControl(new NumericUpDown(), NumericUpDown.ValueProperty, nameof(SampleRate))),
                ("WindowSizeExponent",
                    BoundControl(new NumericUpDown(), NumericUpDown.ValueProperty, nameof(WindowSizePowerOfTwo))),
                ("Window Type",
                    BoundControl(new ComboBox { ItemsSource = Enum.GetNames(typeof(WindowEnum)), IsHitTestVisible = true },
                        SelectingItemsControl.SelectedIndexProperty, nameof(SelectedWindowIndex))))
        ];
    }
}