using System;
using System.Collections.Generic;
using System.Numerics;

namespace AvaloniaReceiver.Filters;

public class FrequencyTransferFunction : IFilter<(double frequency, Complex value), (double frequency, Complex value)>
{
    private IEnumerable<(double frequency, Complex value)>? _source;
    private Func<double, Complex>? _function;

    public void SetTransferFunction(Func<double, Complex> function)
    {
        _function = function;
    }
    
    public void SetInputConnection(IEnumerable<(double frequency, Complex value)> source)
    {
        _source = source;
    }

    public IEnumerable<(double frequency, Complex value)> GetOutputConnection()
    {
        if (_source is null) yield break;
        if (_function is null) yield break;
        
        using var enumerator = _source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            //Console.WriteLine($"{enumerator.Current.frequency}, {_function(enumerator.Current.frequency)}");
            yield return (enumerator.Current.frequency, enumerator.Current.value * _function(enumerator.Current.frequency));
        }
    }
}