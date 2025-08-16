using System;
using System.Numerics;

namespace AvaloniaReceiver.Filters;

public class FrequencyRejectBand : FilterSeries<(double timestamp, double frequency), (double timestamp, double frequency)>
{
    public uint WindowSizePowerOfTwo
    {
        get => FftFilter.WindowSizePowerOfTwo;
        set => FftFilter.WindowSizePowerOfTwo = value;
    }

    public double SampleRate
    {
        get
        {
            if (Math.Abs(FftFilter.SampleRate - InverseFftFilter.SampleRate) > 0.01f)
                InverseFftFilter.SampleRate = FftFilter.SampleRate;
            return InverseFftFilter.SampleRate;
        }
        set
        {
            InverseFftFilter.SampleRate = value;
            FftFilter.SampleRate = value;
        }
    }

    public WindowEnum WindowType
    {
        get => FftFilter.WindowType;
        set => FftFilter.WindowType = value;
    }
    
    private FFTFilter? _fftFilter;
    private FrequencyTransferFunction? _transferFuncFilter;
    private InverseFFTFilter? _inverseFftFilter;
    
    private FFTFilter FftFilter => _fftFilter ??= new FFTFilter(12);
    private FrequencyTransferFunction TransferFuncFilter => _transferFuncFilter ??= new FrequencyTransferFunction();
    private InverseFFTFilter InverseFftFilter => _inverseFftFilter ??= new InverseFFTFilter();
    
    public FrequencyRejectBand() : base()
    {
        AddFilter(FftFilter);
        AddFilter(TransferFuncFilter);
        AddFilter(InverseFftFilter);
        
        TransferFuncFilter.SetTransferFunction(f => f is < 1200 or > 3000? new Complex(1, 1) : 0);
    }
}

