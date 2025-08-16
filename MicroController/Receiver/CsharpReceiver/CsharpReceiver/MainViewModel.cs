using System.IO.Ports;
using PropertyTools.DataAnnotations;

namespace CsharpReceiver;

    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        // try to change might be lower or higher than the rendering interval
        private const int UpdateInterval = 20;

        private bool disposed;
        private readonly Stopwatch watch = new Stopwatch();
        private int numberOfSeries;

        private COMPort dataComPort;
        
        public MainViewModel()
        {
            SetupModel();
        }

        public void ConnectPort(string portName)
        {
            this.dataComPort = new COMPort(portName, DataReadyCallback);
            
            Console.WriteLine("Open COM port");
            
            this.dataComPort.Open();
        }

        private void DataReadyCallback()
        {
            lock (this.PlotModel.SyncRoot)
            {
                this.Update();
            }

            this.PlotModel.InvalidatePlot(true);
        }

        private void SetupModel()
        {
            PlotModel = new PlotModel();
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 4000 });

            this.numberOfSeries = 1;

            for (int i = 0; i < this.numberOfSeries; i++)
            {
                PlotModel.Series.Add(new LineSeries { LineStyle = LineStyle.Solid });
            }

            this.watch.Start();

            this.RaisePropertyChanged("PlotModel");
        }

        public int TotalNumberOfPoints { get; private set; }


        public PlotModel PlotModel { get; private set; }



        private void Update()
        {
            double t = this.watch.ElapsedMilliseconds * 0.001;
            int n = 0;

            foreach (var t1 in PlotModel.Series)
            {
                var s = (LineSeries)t1;
                
                s.Points.AddRange(
                    dataComPort.ReceivedData[0].TakeLast(500).Select(x => new DataPoint(x.timestamp, x.value)));
                
                n += s.Points.Count;
                
                if (n > 10000)
                    s.Points.RemoveRange(0, 500);
            }
            
           

            if (this.TotalNumberOfPoints != n)
            {
                this.TotalNumberOfPoints = n;
                this.RaisePropertyChanged("TotalNumberOfPoints");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                }
            }

            this.disposed = true;
        }
    }
