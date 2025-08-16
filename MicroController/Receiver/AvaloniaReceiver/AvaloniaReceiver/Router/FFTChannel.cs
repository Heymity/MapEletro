using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AvaloniaReceiver.Router;

public class FFTChannel : Channel
{
    private IEnumerable<(double frequency, Complex value)> _source = [];
     
    public void SetFftData(IEnumerable<(double frequency, Complex value)> data)
    {
        Console.WriteLine("FFTChannel.SetFftData {data.Cou");
        _source = data;
    }

    public override IEnumerable<(double timestamp, double value)> GetOutput(bool callProvider = true)
    {
        ChannelData.Reset();

        if (callProvider) ProviderFunc?.Invoke();
        //var evaluatedData = _source.Select(x => (x.frequency, x.value.Real)).ToArray();
        //ChannelData.LastReceivedData.Clear();
        //ChannelData.LastReceivedData.AddRange(evaluatedData);

        while (ChannelData.MoveNext())
        {
            if (ChannelData.Current == (0, 0)) continue;
            yield return ChannelData.Current;
        }
        
        ChannelData.Reset();
        
        /*var evaluatedData = _source.Select(x => (x.frequency, x.value.Real)).ToArray();
        return evaluatedData;
        var magnitudes = FftSharp.FFT.Magnitude(evaluatedData.Select(x => x.value).ToArray());
        var frequencies = evaluatedData.Select(x => x.frequency).ToArray();
        var outputData = frequencies.Zip(magnitudes);
        return outputData;*/
    }
}