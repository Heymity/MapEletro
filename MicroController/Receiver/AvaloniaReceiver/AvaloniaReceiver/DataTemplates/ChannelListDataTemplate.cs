using Avalonia.Media;
using AvaloniaReceiver.Router;

namespace AvaloniaReceiver.DataTemplates;

public class ChannelListDataTemplate
{
    public Channel Channel { get; set; } = new Channel();

    public bool RawDataInput
    {
        get => Channel.IsRaw;
        set => Channel.IsRaw = value;
    }

    public string ChannelName
    {
        get => Channel.ChannelName;
        set => Channel.ChannelName = value;
    }
    
    public int ChannelId => Channel.Id;
    
    public Color ChannelColor { get; set; }
    public bool Enabled { get; set; } = true;
    
    public delegate void ColorChanged(string channelName, Color newColor);
    public delegate void ChannelToggled(string channelName, bool enable);
    public static ColorChanged? OnColorChanged { get; set; } = null;
    public static ChannelToggled? OnChannelToggled { get; set; } = null;
}