using System;
using System.Collections.Generic;
using AvaloniaReceiver.Serial;

namespace AvaloniaReceiver.Router;

public class Channel
{
    private static int _lastId = 0;
    
    public int Id { get; private init; } = _lastId++;
    public string ChannelName { get; set; } = "";
    
    public bool IsRaw { get; set; }
    private DataSource? _channelData;
    public DataSource ChannelData => _channelData ??= new DataSource();

    protected Action? ProviderFunc = null;
    
    public void SetInput(DataSource data)
    {
        _channelData = data;
    }
    
    public void SetRequester(Action getData)
    {
        ProviderFunc = getData;
    }
    
    public virtual IEnumerable<(double timestamp, double value)> GetOutput(bool callProvider = true)
    {
        //Console.WriteLine($"GetOutput() - {_channelData} - {_channelData?.Current} - {callProvider}");
    
        ChannelData.Reset();

        if (!Router.Singleton.IsCached(Id)) ProviderFunc?.Invoke();

        while (ChannelData.MoveNext())
        {
            if (ChannelData.Current == (0, 0)) continue;
            yield return ChannelData.Current;
        }
        
        ChannelData.Reset();
    }

    public override string ToString()
    {
        return $"Channel {ChannelName} ({Id}) - isRaw: {IsRaw}, data: {_channelData}";
    }
}