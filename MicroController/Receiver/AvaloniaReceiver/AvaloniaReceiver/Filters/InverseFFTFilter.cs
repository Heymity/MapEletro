using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FftSharp;

namespace AvaloniaReceiver.Filters;

public class InverseFFTFilter : IFilter<(double frequency, Complex value), (double timestamp, double value)>
{
    public double SampleRate { get; set; } = 10_000d;
    
    private IEnumerable<(double frequency, Complex value)> _source = null!;
    
    //public double SampleRate { get; set; } = 500 / 0.05d;

    public void SetInputConnection(IEnumerable<(double frequency, Complex value)> source)
    {
        _source = source;
    }

    public IEnumerable<(double timestamp, double value)> GetOutputConnection()
    {
        //   Console.WriteLine("get FFT data output");

        //Console.WriteLine(SampleRate);
        //using var enumerator = _source.GetEnumerator();

        var arr = _source.Select(x => x.value).ToArray();
        if (arr.Length == 0) 
            yield break;
    
        //Console.WriteLine(arr.Length);
        var res = InverseReal(arr);
        for (var i = 0; i < arr.Length; i++)
        {
            yield return ( (Router.Router.Singleton.MaxTimestamp - (res.Length-1)/SampleRate)+ i++/SampleRate, res[i]);
        }
    }
    
    private static double[] InverseReal(System.Numerics.Complex[] fft)
    {
        FFT.Inverse(fft);
        double[] Filtered = new double[fft.Length];
        for (int i = 0; i < fft.Length; i++)
            Filtered[i] = fft[i].Real;
        return Filtered;
    }
}