using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FftSharp;
using FftSharp.Windows;

namespace AvaloniaReceiver.Filters;

// ReSharper disable once InconsistentNaming
public class FFTFilter(uint windowSizeExponent) : IFilter<(double timestamp, double value), (double frequency, Complex value)>
{
    private IEnumerable<(double timestamp, double value)> _source = null!;

    public uint WindowSizePowerOfTwo
    {
        get => _windowSizePowerOfTwo;
        set
        {
            _windowSizePowerOfTwo = value;
            _take = 1 << (int)_windowSizePowerOfTwo;
            _data = new double[1 << (int)_windowSizePowerOfTwo];
        }
    }

    public double SampleRate { get; set; } = 500/0.05d;

    public WindowEnum WindowType
    {
        get => _windowType;
        set
        {
            _windowType = value;
            _window = WindowFromEnum(value);
        }
    }

    private Window _window = new Rectangular();

    public void SetInputConnection(IEnumerable<(double timestamp, double value)> source)
    {
        _source = source;
    }

    private int _index = 0;
    private int _take = 1 << (int)windowSizeExponent; 
    private double[] _data = new double[1 << (int)windowSizeExponent];
    private uint _windowSizePowerOfTwo = windowSizeExponent;
    private WindowEnum _windowType = WindowEnum.Hanning;

    public IEnumerable<(double frequency, Complex value)> GetOutputConnection()
    {
     //   Console.WriteLine("get FFT data output");
        
        //Console.WriteLine(SampleRate);
        using var enumerator = _source.GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (_index < _take)
            {
                _data[_index++] = enumerator.Current.value;
                continue;
            }
            
            _window.ApplyInPlace(_data);

            foreach (var fftPair in FftSharp.FFT.FrequencyScale(_data.Length, SampleRate, true).Zip(FftSharp.FFT.Forward(_data)))
            {
                yield return fftPair;
            }

            _index = 0;
        }
    }

    private Window WindowFromEnum(WindowEnum windowEnum) =>
        windowEnum switch
        {
            WindowEnum.Bartlett => new Bartlett(),
            WindowEnum.Blackman => new Blackman(),
            WindowEnum.Cosine => new Cosine(),
            WindowEnum.FlatTop => new FlatTop(),
            WindowEnum.Hamming => new Hamming(),
            WindowEnum.HammingPeriodic => new HammingPeriodic(),
            WindowEnum.Hanning => new Hanning(),
            WindowEnum.HanningPeriodic => new HanningPeriodic(),
            WindowEnum.Kaiser => new Kaiser(),
            WindowEnum.Rectangular => new Rectangular(),
            WindowEnum.Turkey => new Tukey(),
            WindowEnum.Welch => new Welch(),
            _ => throw new ArgumentOutOfRangeException(nameof(windowEnum), windowEnum, null)
        };
}

public enum WindowEnum
{
    Bartlett,
    Blackman,
    Cosine,
    FlatTop,
    Hamming,
    HammingPeriodic,
    Hanning,
    HanningPeriodic,
    Kaiser,
    Rectangular,
    Turkey,
    Welch,
}   