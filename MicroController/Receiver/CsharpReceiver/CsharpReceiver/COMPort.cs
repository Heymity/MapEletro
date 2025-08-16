using System.Text;
using System.Windows.Documents;

namespace CsharpReceiver;

using System.IO.Ports;

public class COMPort
{
    private SerialPort _serialPort;
    private ulong _lastTimestamp = 0;
    
    private List<(double timestamp, ushort value)>[] _receivedData = new List<(double, ushort)>[256];
    
    public List<(double timestamp, ushort value)>[] ReceivedData => _receivedData;
    public static string[] GetPortNames() => SerialPort.GetPortNames();

    public delegate void DataReadyEventHandler();
    public DataReadyEventHandler DataReady;

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var sp = (SerialPort)sender;

        while (sp.BytesToRead > 0)
        {
            Console.WriteLine(sp.BytesToRead);
            
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
            var numChn = headerBytes[9];
            
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
                if (chn >= numChn) chn = 0;
                
                if (!BitConverter.IsLittleEndian) dataSpan.Slice(i, 2).Reverse();
                var uint16Data = BitConverter.ToUInt16(dataSpan.Slice(i, 2));
                
                _receivedData[chn].Add((timestamp - (sampleTime * (1 - (i/2000d))), uint16Data));

                chn++;
            }
            
            Console.WriteLine($"Received {packetLength} bytes from {sp.PortName}, with timestamp {timestamp}, first chn {firstChn} and {numChn} channels, timestamp delta was {timestamp - _lastTimestamp}us, Sample time was {sampleTime}us");

            
            _lastTimestamp = timestamp;
        }
        
        DataReady?.Invoke();
    }

    public COMPort(string portName, DataReadyEventHandler dataReadyCallback)
    {
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
            _receivedData[i] = [];
        }
        
        DataReady = dataReadyCallback;
    }

    public void Open()
    {
        _serialPort.Open();
    }

    public void Close()
    {
        _serialPort.Close();
    }
    
    
}