using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaReceiver.Filters;

public static class FilterUtility
{
    public static void SetInputConnection(this IFilter filter, IEnumerable input)
    {
        var (newFilterTIn, newFilterTOut) = filter.GetGenericTypes();
        
        var interfaceType = typeof(IFilterIn<>).MakeGenericType(newFilterTIn);
        if (interfaceType.IsInstanceOfType(filter))
        {
            interfaceType.GetMethod("SetInputConnection")!.Invoke(filter, [input]);
            return;
        }
        else throw new ArgumentException($"Filter has entry type {newFilterTIn}, but doesn't implement IFilterIn<T>");
    }
    
    public static IEnumerable GetOutputConnection(this IFilter filter)
    {
        var (newFilterTIn, newFilterTOut) = filter.GetGenericTypes();
        
        var interfaceType = typeof(IFilterOut<>).MakeGenericType(newFilterTOut);
        if (interfaceType.IsInstanceOfType(filter))
        {
            var result = interfaceType.GetMethod("GetOutputConnection")!.Invoke(filter, []);
            if (result is null) throw new ArgumentException("Result from output should not be null");
            return (IEnumerable)result;
        }
        else throw new ArgumentException($"Filter has exit type {newFilterTOut}, but doesn't implement IFilterOut<T>");
    }
    
    
    public static (Type TIn, Type TOut) GetGenericTypes(this IFilter filter)
    {
        var result = GetGenericTypesNoError(filter);
        if (result.TIn is null || result.TOut is null) throw new Exception("Incorrect filter declaration");
        
        return result!;
    }
    
    public static (Type? TIn, Type? TOut) GetGenericTypesNoError(this IFilter filter)
    {
        /*foreach (var @interface in filter.GetType().GetInterfaces())
        {
            Console.WriteLine(@interface);
        }*/
        
        var interfaceType = filter.GetType().GetInterfaces().FirstOrDefault(i => typeof(IFilter).IsAssignableFrom(i) && i.GenericTypeArguments.Length == 2);
        if (interfaceType is null) return (null, null);
        var genericArguments = interfaceType.GetGenericArguments(); 
        return (genericArguments[0], genericArguments[1]);
    }
}