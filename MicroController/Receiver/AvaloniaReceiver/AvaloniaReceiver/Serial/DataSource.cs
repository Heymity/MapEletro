using System.Collections;
using System.Collections.Generic;

namespace AvaloniaReceiver.Serial;

public class DataSource : IEnumerator<(double timestamp, double value)>
{
    public List<(double timestamp, double value)> LastReceivedData { get; set; } = [];
    
    private int _index = -1;
    public bool MoveNext()
    {
        return ++_index < LastReceivedData.Count;
    }

    public void Reset()
    {
        _index = -1;
    }

    public (double timestamp, double value) Current => _index < 0 ? (0,0) : LastReceivedData[_index];

    object? IEnumerator.Current => Current;

    public void Dispose()
    {
        
    }
    
}