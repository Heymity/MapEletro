using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaReceiver.Filters;

public class DecimateFilter<T>(int decimationFactor) : IFilter<T>
{
    //public double OriginalMaxFrequency { get; set; }
    public int DecimationFactor { get; set; } = decimationFactor;

    //public double MaxAntiAliasFilterCutoff => 0.5d * OriginalMaxFrequency/DecimationFactor;

    private IEnumerable<T> _source = null!;
    private int _skipped = 0;
    
    public void SetInputConnection(IEnumerable<T> source)
    {
        _source = source;
    }

    public IEnumerable<T> GetOutputConnection()
    {
        using var enumerator = _source.GetEnumerator();
        
        while (enumerator.MoveNext())
        {
            if (_skipped++ >= DecimationFactor - 1)
            {
                _skipped = 0;
                yield return enumerator.Current;
            }
        }
    }
}