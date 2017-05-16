using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    class Plot
    {
        private PlotModel plotModel;
        private float range;

        public PlotModel PlotModel { get => plotModel; }

        /// <summary>
        /// Creates a plot for the view. 
        /// 
        /// If the number data points for one line series reaches the max threshold, 
        /// all older data points will not be shown.
        /// </summary>
        /// <param name="range">Range of values along the x axis.</param>
        public Plot(float range)
        {
            this.range = range;
        }

        /// <summary>
        /// Draws a plot of given data points. 
        /// 
        /// The RaisePropertyChanged event must be raised after the update to refresh the plot view.
        /// </summary>
        /// <param name="dataPoints">Set of data points (x,y). The Key will be used as title of the line series.</param>
        /// <param name="title">Title of the plot.</param>
        public void UpdatePlot(Dictionary<String, List<Double[]>> dataPoints, String title)
        {
            PlotModel tmp = new PlotModel { Title = title != null ? title : "" };
            LinearAxis xAxis = new LinearAxis();
            xAxis.Position = AxisPosition.Bottom;
            double maxXValue = 0;            

            foreach (KeyValuePair<String, List<Double[]>> series in dataPoints) {
                LineSeries lineSeries = new LineSeries { Title = series.Key, MarkerType = MarkerType.Circle };

                int indexCount = series.Value.Count();
                //int indexStart = indexCount > maxValues ? indexCount - maxValues : 0;
                //for (int j = indexStart; j < indexCount; j++) {
                for (int j = 0; j < indexCount; j++) {
                    maxXValue = maxXValue < series.Value[j][0] ? series.Value[j][0] : maxXValue;
                    lineSeries.Points.Add(new DataPoint(series.Value[j][0], series.Value[j][1]));
                }

                tmp.Series.Add(lineSeries);
            }

            // set graph range by highest value from all data Points
            xAxis.Minimum = maxXValue - range;
            tmp.Axes.Add(xAxis);
            plotModel = tmp;
        }
    }
}
