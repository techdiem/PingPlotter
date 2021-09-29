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

namespace PingPlotter
{
    public partial class GraphWindow : Window, INotifyPropertyChanged
    {
        private double _axisMax;
        private double _axisMin;
        private string hostInfo = "";
        private IPAddress hostIP;
        private int pingCounter = 0;
        private int lossCounter = -1;
        private string csvPath = "";
        private TextWriter swrt;

        public GraphWindow(string hostInformation, string CSVFilePath)
        {
            hostInfo = hostInformation;
            csvPath = CSVFilePath;
            InitializeComponent();

            Title = string.Concat(hostInfo, " - PingPlotter");

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

            //the values property will store our values array
            ChartValues = new ChartValues<MeasureModel>();

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);


            IsReading = true;
            Task.Factory.StartNew(Read);

            DataContext = this;
        }

        public ChartValues<MeasureModel> ChartValues { get; set; }
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

                PingReply pingResult = PingUtil.PingHost(hostIP);
                string logResult;
                pingCounter++;

                if (pingResult == null || pingResult.Status != IPStatus.Success)
                {
                    lossCounter++;
                    logResult = "lost";
                }
                else
                {
                    ChartValues.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = pingResult.RoundtripTime
                    });
                    logResult = pingResult.RoundtripTime.ToString();
                }

                lblPacketLoss.Dispatcher.Invoke(new Action(() =>
                {
                    lblPacketLoss.Content = string.Concat("Paketverlust: ", lossCounter);
                }));
                SetAxisLimits(now);

                //lets only use the last 150 values
                if (ChartValues.Count > 150) ChartValues.RemoveAt(0);

                //Log to CSV if enabled
                if (swrt != null)
                {
                    swrt.WriteLine("{0},{1},{2}", pingCounter.ToString(), now.TimeOfDay, logResult);
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


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            hostIP = PingUtil.GetIpFromHost(ref hostInfo);
            if (csvPath != null)
            {
                swrt = new StreamWriter(csvPath);
            }
        }

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
