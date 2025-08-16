#undef SIMULATE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Avalonia.Threading;


namespace AvaloniaReceiver.Serial;



public class ComPort : IDisposable
{
    private readonly SerialPort _serialPort;
    private ulong _lastTimestamp = 0;
    
    private readonly List<(double timestamp, double value)>[] _receivedData = new List<(double, double)>[256];

    public double CurrentTimestamp { get; private set; }
    
    #if !SIMULATE
    public bool Connected => _serialPort.IsOpen;
#else
    public bool Connected {get; private set;}
    #endif
    
    public DataSource[] DataSources { get; } = new DataSource[256];

    public List<(double timestamp, double value)>[] ReceivedData => _receivedData;
    public byte NumChannels { get; private set; }
    
    
    private static readonly string[] NoPorts = ["Sem porta serial"];
    public static string[] ReadableListPorts 
    {
        get
        {
            var ports = SerialPort.GetPortNames();

            return ports.Length == 0 ? NoPorts : ports;
        }
    }

    public delegate void DataReadyEventHandler();
    public DataReadyEventHandler DataReady;

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var sp = (SerialPort)sender;
        
        foreach (var t in _receivedData)
            t.Clear();
        
        while (sp.BytesToRead > 0)
        {
            //Console.WriteLine(sp.BytesToRead);
            
            var bytes = new byte[4];
            sp.Read(bytes, 0, 4);

            var buf = bytes.AsSpan();

            //Console.WriteLine(buf[0..4].ToArray().Select(x => x.ToString("X2")).Aggregate((a, b) => a + "," + b));
            if (!(buf[0] == 0x44 && buf[1] == 0x41 && buf[2] == 0x54 && buf[3] == 0x41))
            {
                // Not start of a packet
                sp.DiscardInBuffer();
                return;
            }
            
            var headerBytes = new byte[16];
            sp.Read(headerBytes, 0, headerBytes.Length);
            var headerSpan = headerBytes.AsSpan();

            var timestampBytes = headerSpan[0..8];
            if (!BitConverter.IsLittleEndian) timestampBytes.Reverse();
            var timestamp = BitConverter.ToUInt64(timestampBytes.ToArray(), 0);

            var firstChn = headerBytes[8];
            NumChannels = headerBytes[9];
            
            var packetLengthBytes = headerSpan[10..12];
            if (!BitConverter.IsLittleEndian) packetLengthBytes.Reverse();
            var packetLength = BitConverter.ToUInt16(packetLengthBytes.ToArray(), 0);
            
            var sampleTimeBytes = headerSpan[12..];
            if (!BitConverter.IsLittleEndian) sampleTimeBytes.Reverse();
            var sampleTime = BitConverter.ToUInt32(sampleTimeBytes.ToArray(), 0);
            
            var data = new byte[packetLength];
            sp.Read(data, 0, packetLength);

            if (_lastTimestamp == 0)
            {
                _lastTimestamp = timestamp;
                continue;
            }
            
            var dataSpan = data.AsSpan();
            var dataLen = packetLength / 2.0d;
            var step = sampleTime / dataLen;
            var chn = firstChn;
            for (var i = 0; i < packetLength; i+=2)
            {
                if (chn >= NumChannels) chn = 0;
                
                if (!BitConverter.IsLittleEndian) dataSpan.Slice(i, 2).Reverse();
                var uint16Data = BitConverter.ToUInt16(dataSpan.Slice(i, 2));
                
                // ADC -> UINT16 (RP2350)
                // 0    - 0
                // 3,3V - 4096
                // 
                // Desmodulador -> ADC (AmpOp somador inversor)
                // -5       - 3,8
                // -3,616   - 3,3
                // 5        - 0
                // Rf = 18K
                // Ra = 47K
                // Vadc = -(V - 5) * 18/47
                _receivedData[chn].Add(((timestamp - (sampleTime * (1 - (i/2000d))))/1_000_000d, 5 - (47d*(3.3d*uint16Data/4096d)/18d)));

                //if (_receivedData[chn].Count >= 10000)
                    //_receivedData[chn].RemoveRange(0, _receivedData[chn].Count-10000);
                
                chn++;
            }
            
            //Console.WriteLine($"Received {packetLength} bytes from {sp.PortName}, with timestamp {timestamp}, first chn {firstChn} and {NumChannels} channels, timestamp delta was {timestamp - _lastTimestamp}us, Sample time was {sampleTime}us");


            if (Math.Abs((float)((timestamp - _lastTimestamp) - sampleTime)) > 10f)
            {
                Console.WriteLine($"Possible package loss detected, timestamp delta was {(timestamp - _lastTimestamp)} while sample time was {sampleTime}");
            }
            
            _lastTimestamp = timestamp;
        }

        for (int i = 0; i < _receivedData.Length; i++)
        {
            DataSources[i].LastReceivedData.Clear();
            DataSources[i].Reset();
            DataSources[i].LastReceivedData.AddRange(_receivedData[i].Select(x => (x.timestamp, x.value)));
        }

        CurrentTimestamp = _receivedData.Aggregate(new List<double>(), (acc, cur) =>
        {
            if (cur.Count == 0) acc.Add(0);       
            else acc.Add(cur.Select(x => x.timestamp).Max());
            return acc;
        }).Max();
        
        DataReady?.Invoke();
    }
    
    #if SIMULATE
    private double _time = 0;
    private readonly DispatcherTimer _timer;
    #endif
    
    public ComPort(string portName, DataReadyEventHandler dataReadyCallback)
    {
#if !SIMULATE
        _serialPort = new SerialPort(portName);
        _serialPort.BaudRate = 115200;
        _serialPort.Parity = Parity.None;
        _serialPort.DataBits = 8;
        _serialPort.StopBits = StopBits.One;
        _serialPort.Handshake = Handshake.None;
        _serialPort.DtrEnable = true;
        _serialPort.RtsEnable = false;

        _serialPort.ReadTimeout = SerialPort.InfiniteTimeout;
        _serialPort.DataReceived += SerialPortOnDataReceived;

        for (int i = 0; i < _receivedData.Length; i++)
        {
            _receivedData[i] = new List<(double timestamp, double value)>(i <= 3 ? 5000 : 1);
            DataSources[i] = new DataSource();
        }
        
        DataReady = dataReadyCallback;
#else 
        for (int i = 0; i < _receivedData.Length; i++)
        {
            _receivedData[i] = new List<(double timestamp, ushort value)>(i <= 3 ? 5000 : 1);
            DataSources[i] = new DataSource();
        }
        
        DataReady = dataReadyCallback;

        NumChannels = 2;

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Default, Callback);
        _timer.Start();
        
#endif
    }
    
    #if SIMULATE
    void Callback(object? sender, EventArgs e)
    {
        foreach (var t in _receivedData)
            t.Clear();
            
        for (var i = 0; i < 500; i++)
        {
            _receivedData[0].Add((_time, (ushort)(1500 * (Math.Sin(3*Math.PI*_time) + 1))));
            _receivedData[1].Add((_time, (ushort)(1500 * (Math.Sin(3*Math.PI*_time + Math.PI/2) + 1))));
            _time += 0.05d / 500;
        }
            
        for (int i = 0; i < _receivedData.Length; i++)
        {
            DataSources[i].LastReceivedData.Clear();
            DataSources[i].Reset();
            DataSources[i].LastReceivedData.AddRange(_receivedData[i].Select(x => (x.timestamp, (double)x.value)));
        }
            
        DataReady();
    }
    #endif

    public void Open()
    {
#if !SIMULATE
        _serialPort.Open();
        #else
        Connected = true;
#endif
    }

    public void Close()
    {
#if !SIMULATE
        _serialPort.Close();
        #else
        Connected = false;
#endif
    }
    
    public void Dispose()
    {
#if !SIMULATE
        if (_serialPort.IsOpen) _serialPort.Close();
        _serialPort.Dispose();
#endif
    }
}