using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AvaloniaReceiver.Router;

namespace AvaloniaReceiver.Filters;

public class OutputFFTChannelFilter : IFilter<(double frequency, Complex value)>
{
    private IEnumerable<(double frequency, Complex value)>? _rawSource;
    
    public FFTChannel? OutputChannel { get; set; }
    
    public void SetInputConnection(IEnumerable<(double frequency, Complex value)> source)
    {
        _rawSource = source;
    }

    public IEnumerable<(double frequency, Complex value)> GetOutputConnection()
    {
        //Console.WriteLine("Output To channel");
        if (OutputChannel is null || _rawSource is null) return [];
        
        //Console.WriteLine($"Output To channel2 {_rawSource.Count()}");
        OutputChannel.ChannelData.LastReceivedData.Clear();
        //FftSharp.FFT.Magnitude(_rawSource.Select(x => x.value).ToArray());
        var fftArr = _rawSource.Select(x => x.value).ToArray();
        if (fftArr.Length == 0) return new[] { (-10d, new Complex(-10, 0d)) };
        
        //Console.WriteLine(fftArr.Length);
        var arr = FftSharp.FFT.Power(fftArr, true);
        if (arr is null)
        {
            Console.WriteLine("No FFT data");
            return new []{ (-10d, new Complex(-20, 0d))};
        }
        OutputChannel.ChannelData.LastReceivedData.AddRange(FftSharp.FFT.FrequencyScale(arr.Length, 10_000).Zip(arr));
        //OutputChannel.SetFftData(_rawSource);
        //Console.WriteLine($"get fft3 {arr.Length}");
        return new[] { (-10d, new Complex(-10, 0d)) };
    }

    public override string ToString()
    {
        return $"OutputFFTChannelFilter {(_rawSource is null ? "null Source" : _rawSource.Count().ToString())}";
    }
}