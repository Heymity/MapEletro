using System;
using System.Collections;
using System.Collections.Generic;

namespace AvaloniaReceiver.Filters;

public interface IFilter
{
   
}

public interface IFilterOut<out T> : IFilter
{
    public IEnumerable<T> GetOutputConnection();
}

public interface IFilterIn<in T> : IFilter
{
    public void SetInputConnection(IEnumerable<T> source);
}

public interface IFilter<in TIn, out TOut> : IFilterIn<TIn>, IFilterOut<TOut>;

public interface IFilter<T> : IFilter<T, T>;