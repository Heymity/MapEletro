using System.Collections.Generic;
using System.Linq;

namespace AvaloniaReceiver.Filters;

public class PassAllFilter<T> : IFilter<T>
{
    private IEnumerable<T> _collection = [];

    public void SetInputConnection(IEnumerable<T> source)
    {
        _collection = source;
    }

    public IEnumerable<T> GetOutputConnection()
    {
        return _collection;
    }
}