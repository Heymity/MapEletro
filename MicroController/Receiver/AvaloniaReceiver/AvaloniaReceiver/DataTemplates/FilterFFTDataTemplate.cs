using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaReceiver.Filters;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

// ReSharper disable once InconsistentNaming
public class FilterFFTDataTemplate(string filterName, Action<int> removeBtn, Router.Router router) : FilterDataTemplate(filterName, removeBtn, router)
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

    private FFTFilter Filter => _filter ??= new FFTFilter(12);

    [UsedImplicitly]
    public FilterFFTDataTemplate(Router.Router router) : this("FFT Filter", _ => { }, router) { }

    private FFTFilter? _filter;

    public override IFilter GetFilter() => Filter;

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