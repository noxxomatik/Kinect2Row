using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    public class Plot
    {
        private PlotModel plotModel;
        private float range;
        private Dictionary<String, OxyColor> colors;
        private double maxXValue = 0;

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

        public PlotModel PlotModel { get => plotModel; private set => plotModel = value; }
        public Dictionary<string, OxyColor> Colors { get => colors; set => colors = value; }

        /// <summary>
        /// Draws a plot of given data points. 
        /// 
        /// The RaisePropertyChanged event must be raised after the update to refresh the plot view.
        /// </summary>
        /// <param name="dataPoints">Set of data points (x,y). The Key will be used as title of the line series.</param>
        /// <param name="title">Title of the plot.</param>
        public void UpdatePlot(Dictionary<String, List<Double[]>> dataPoints,
            String title,
            Dictionary<String, OxyColor> colors = null)
        {
            PlotModel tmp = new PlotModel { Title = title != null ? title : "" };
            LinearAxis xAxis = new LinearAxis();
            xAxis.Position = AxisPosition.Bottom;
            double maxXValue = 0;

            foreach (KeyValuePair<String, List<Double[]>> series in dataPoints) {
                LineSeries lineSeries = new LineSeries {
                    Title = series.Key,
                    MarkerType = MarkerType.Circle
                };

                // check if specific colors are set
                if (colors != null && colors.Count() == dataPoints.Count()) {
                    lineSeries.Color = colors[series.Key];
                }

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
            PlotModel = tmp;
        }

        public void Init(String title, Dictionary<String, OxyColor> colors = null)
        {
            PlotModel tmp = new PlotModel { Title = title != null ? title : "" };

            // colors
            if (colors != null) {
                Colors = colors;
            }

            // axis
            LinearAxis xAxis = new LinearAxis();
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Minimum = maxXValue - range;
            tmp.Axes.Add(xAxis);

            PlotModel = tmp;
        }

        public void AddDataPoint(string series, double[] values)
        {
            // check if series exists
            LineSeries existingSeries = null;
            foreach (Series ser in PlotModel.Series) {
                if (ser != null && ser.Title == series) {
                    existingSeries = (LineSeries) ser;
                    break;
                }
            }
            if (existingSeries != null) {
                // add values to the series
                existingSeries.Points.Add(new DataPoint(values[0], values[1]));
            }
            else {
                // create series and add point
                LineSeries lineSeries = new LineSeries {
                    Title = series,
                    MarkerType = MarkerType.Circle
                };
                lineSeries.Points.Add(new DataPoint(values[0], values[1]));
                // color
                if (Colors != null && Colors.ContainsKey(series)) {
                    lineSeries.Color = Colors[series];
                }
                PlotModel.Series.Add(lineSeries);
            }
            // renew the axis minimum
            maxXValue = maxXValue < values[0] ? values[0] : maxXValue;
            foreach (Axis axis in PlotModel.Axes) {
                if (axis.Position.Equals(AxisPosition.Bottom)) {
                    axis.Minimum = maxXValue - range;
                }
            }
            // refresh the plot
            PlotModel.InvalidatePlot(true);
        }
    }
}
