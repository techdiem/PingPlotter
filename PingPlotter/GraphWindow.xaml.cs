using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Net;
using LiveCharts;
using LiveCharts.Configurations;
using System.Net.NetworkInformation;
using LiveCharts.Wpf;

namespace PingPlotter
{
    public partial class GraphWindow : Window, INotifyPropertyChanged
    {
        private double _axisMax;
        private double _axisMin;
        private int pingCounter = 0;
        private int lossCounter = -1;
        private TextWriter swrt;
        private IPAddress[] hostIP;

        public GraphWindow(string hostInformation, string CSVFilePath)
        {
            InitializeComponent();

            Title = string.Concat(hostInformation, " - PingPlotter");

            //To handle live data easily, in this case we built a specialized type
            //the MeasureModel class, it only contains 2 properties
            //DateTime and Value
            //We need to configure LiveCharts to handle MeasureModel class
            //The next code configures MeasureModel  globally, this means
            //that LiveCharts learns to plot MeasureModel and will use this config every time
            //a IChartValues instance uses this type.
            //this code ideally should only run once
            //you can configure series in many ways, learn more at 
            //http://lvcharts.net/App/examples/v1/wpf/Types%20and%20Configuration

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);
            
            //Initialize SeriesCollection to store ping data of multiple hosts
            SeriesCollection = new SeriesCollection {};

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            string[] hosts = hostInformation.Split(',');
            hostIP = new IPAddress[hosts.Length];

            for (int i = 0; i < hosts.Length; i++)
            {
                hostIP[i] = PingUtil.GetIpFromHost(hosts[i]);

                SeriesCollection.Add(new LineSeries
                {
                    Title = hosts[i],
                    Values = new ChartValues<MeasureModel> { },
                    Fill = System.Windows.Media.Brushes.Transparent,
                    LineSmoothness = 0,
                });
            }


            //Initialize CSV writer
            if (CSVFilePath != null)
            {
                swrt = new StreamWriter(CSVFilePath);
                swrt.WriteLine("Ping,Time,{0}", string.Join(",", hosts));
            }


            IsReading = true;
            Task.Factory.StartNew(Read);

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string ChartLegend {
            get
            {
                if (hostIP.Length > 1) {
                    return "Right";
                }
                else
                {
                    return "None";
                }
            }
        }

        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }

        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }

        public bool IsReading { get; set; }

        private void Read() {
            while (IsReading) {
                DateTime now = DateTime.Now;

                string[] logResult = new string[hostIP.Length];
                int ipindex = 0;

                foreach (IPAddress ip in hostIP)
                {
                    PingReply pingResult = PingUtil.PingHost(ip);

                    if (pingResult == null || pingResult.Status != IPStatus.Success)
                    {
                        lossCounter++;
                        logResult[ipindex] = "lost";
                    }
                    else
                    {
                        SeriesCollection[ipindex].Values.Add(
                            new MeasureModel
                            {
                                DateTime = now,
                                Value = pingResult.RoundtripTime
                            });

                        logResult[ipindex] = pingResult.RoundtripTime.ToString();
                    }

                    ipindex++;
                }

                
                lblPacketLoss.Dispatcher.Invoke(new Action(() =>
                {
                    lblPacketLoss.Content = string.Concat("Paketverlust: ", lossCounter);
                }));
                SetAxisLimits(now);

                //only use the last 150 values
                if (SeriesCollection[0].Values.Count > 150)
                {
                    for (int i = 0; i < SeriesCollection.Count; i++)
                    {
                        SeriesCollection[i].Values.RemoveAt(0);
                    }
                }

                pingCounter++;
                //Log to CSV if enabled
                if (swrt != null)
                {
                    swrt.WriteLine("{0},{1},{2}", pingCounter.ToString(), now.TimeOfDay, string.Join(",", logResult));
                    if (pingCounter % 20 == 0)
                    {
                        swrt.FlushAsync();
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(30).Ticks; // and 30 seconds behind
        }

        private void InjectStopOnClick(object sender, RoutedEventArgs e)
        {
            IsReading = !IsReading;
            if (IsReading) Task.Factory.StartNew(Read);
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (swrt != null)
            {
                IsReading = false;
                swrt.Close();
            }
        }
    }
}
