using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.VisualBasic;

namespace AvaloniaReceiver.Filters;

public interface IFilterSeries : IFilter
{
    public IFilterSeries AddFilter(IFilter filter);
    IEnumerable<IFilter> GetFilters();
}

public class FilterSeries<TIn, TOut> : IFilter<TIn, TOut>, IFilterSeries
{
    private readonly List<IFilter> _filters = [];

    
    public IFilterSeries AddFilter(IFilter filter)
    {
        if (_filters.Count > 0)
        {
            var lastFilter = _filters.Last();
            var (_, lastTOut) = lastFilter.GetGenericTypes();
            var (newFilterTIn, newFilterTOut) = filter.GetGenericTypes();
            Console.WriteLine($"Filter: {lastTOut} ({newFilterTIn} -> {newFilterTOut}");
            if (newFilterTIn != lastTOut) throw new ArgumentException($"The last added filter has output port of type {lastTOut}, while the added filter has input port of type {newFilterTIn}.");

            var addFilterGenericMethod = this
                .GetType()
                .GetMethods()
                .First(m => m is { Name: "AddFilter", IsGenericMethod: true })
                .MakeGenericMethod(lastTOut, newFilterTOut);
            
            addFilterGenericMethod.Invoke(this, [filter]);

            return this;
            
            /*var interfaceType = typeof(IFilterIn<>).MakeGenericType(lastTOut);
            if (interfaceType.IsInstanceOfType(filter))
            {
                var lastInterfaceType = typeof(IFilterOut<>).MakeGenericType(lastTOut);
                if (!lastInterfaceType.IsInstanceOfType(lastFilter)) throw new ArgumentException("Yeah.... This shouldn't happen lol");
                
                var result = lastInterfaceType.GetMethod("GetOutputConnection")!.Invoke(lastFilter, []);
                if (result is null) throw new ArgumentException("Result from output should not be null");
                interfaceType.GetMethod("SetInputConnection")!.Invoke(filter, [result]);
            }
            else throw new ArgumentException($"Last filter has exit type {lastTOut.Name}, while the added filter input is {newFilterTIn.Name}");*/
        }

        _filters.Add(filter);

        return this;
    }

    public IEnumerable<IFilter> GetFilters()
    {
        return _filters.AsReadOnly();
    }

    [UsedImplicitly]
    public FilterSeries<TIn, TOut> AddFilter<TFIn, TFOut>(IFilter<TFIn, TFOut> filter)
    {
        if (_filters.Count > 0)
        {
            var lastFilter = _filters.Last();
            if (lastFilter is IFilterOut<TFIn> lastFilterOut)
                filter.SetInputConnection(lastFilterOut.GetOutputConnection());
            else throw new ArgumentException($"Last filter has exit type {lastFilter.GetGenericTypes().TOut.Name}, while the added filter input is {typeof(TFIn)}");
        }

        _filters.Add(filter);

        return this;
    }

    public void SetInputConnection(IEnumerable<TIn> source)
    {
        if (_filters.Count == 0) return;
        
        var firstFilter = _filters.First();
        if (firstFilter is IFilterIn<TIn> firstFilterIn)
            firstFilterIn.SetInputConnection(source);
        else throw new ArgumentException($"First filter has entry type {firstFilter.GetGenericTypes().TIn.Name}, while the input type is {typeof(TIn)}");
    }

    public IEnumerable<TOut> GetOutputConnection()
    {
        Console.WriteLine("GetOutputConnection");
        if (_filters.Count == 0) return Array.Empty<TOut>();
        
        var lastFilter = _filters.Last();
        if (lastFilter is IFilterOut<TOut> lastFilterOut)
            return lastFilterOut.GetOutputConnection();
        else throw new ArgumentException($"Last filter has exit type {lastFilter.GetGenericTypes().TOut.Name}, while {typeof(TOut)} was expected");
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine("FilterSeries: (" + _filters.Count + ")");
        foreach (var filter in _filters)
        {
            builder.AppendLine(filter.ToString());
        }
        
        return builder.ToString();
    }
}