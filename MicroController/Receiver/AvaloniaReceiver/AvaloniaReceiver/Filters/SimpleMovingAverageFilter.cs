using System;
using System.Collections.Generic;

namespace AvaloniaReceiver.Filters;

public class SimpleMovingAverageFilter(int averageSize) : IFilter<(double timestamp, double value)>
{
    private IEnumerable<(double timestamp, double value)> _source = null!;

    public int AverageSize
    {
        get => _averageSize;
        set
        {
            _averageSize = value;
            _arrNum = new double[_averageSize];
            _runningSum = 0;
            _sumIndex = 0;
        }
    }

    public void SetInputConnection(IEnumerable<(double, double)> source)
    {
        Console.WriteLine("InputSet");
        _source = source;
    }

    private double[] _arrNum = new double[averageSize];
    private double _runningSum = 0;
    private int _sumIndex = 0;
    private int _averageSize = averageSize;

    private double _last;
    public IEnumerable<(double, double)> GetOutputConnection()
    {
        using var enumerator = _source.GetEnumerator();
        //Console.WriteLine($"_sumIndex: {_sumIndex}, _sum: {_runningSum}, _averageSize: {_averageSize}, arr: {_arrNum.Length}, _last: {_last}");
        
        while (enumerator.MoveNext())
        {
            if (_sumIndex >= AverageSize) _sumIndex = 0;
            _runningSum = _runningSum - _arrNum[_sumIndex] + enumerator.Current.value;

            _arrNum[_sumIndex++] = enumerator.Current.value;
            
            _last = _runningSum / AverageSize;
            
            //Console.WriteLine(enumerator.Current.timestamp);
            yield return (enumerator.Current.timestamp, _runningSum/AverageSize);
        }
    }
}