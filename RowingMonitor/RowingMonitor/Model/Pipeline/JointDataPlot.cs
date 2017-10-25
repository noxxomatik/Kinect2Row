using OxyPlot;
using RowingMonitor.Model.Util;
using RowingMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using RowingMonitor.View;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using Microsoft.Kinect;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RowingMonitor.Model.Pipeline
{
    class JointDataPlot
    {
        private ActionBlock<JointData> plotJointDataBlock;

        private ActionBlock<List<SegmentHit>> plotHitsBlock;

        private float range;

        // plot view
        private JointDataPlotView view;
        private JointDataPlotViewModel viewModel;

        // plot options
        private List<JointType> plotJointTypes = new List<JointType>();
        private List<DataStreamType> plotMeasuredVariables = new List<DataStreamType>();

        // data for the plot
        private ConcurrentDictionary<string, List<PlotData>> plotData =
            new ConcurrentDictionary<string, List<PlotData>>();

        // hits count for comparison
        private int oldHitsCount = 0;

        /// <summary>
        /// Creates a plot for joint data.
        /// 
        /// The range determines the maximum difference between the last x value
        /// and the first x value shown.
        /// </summary>
        /// <param name="range">Range of values along the x axis.</param>
        public JointDataPlot(float range = 0.0f)
        {
            Range = range;

            // create a new view
            View = new JointDataPlotView();
            viewModel = (JointDataPlotViewModel)View.DataContext;

            PlotJointDataBlock = new ActionBlock<JointData>(jointData =>
            {
                if (AddPlotData(jointData)) {
                    viewModel.Update(UpdatePlot(plotData, "Values"));
                }
            });

            PlotHitsBlock = new ActionBlock<List<SegmentHit>>(hits =>
            {
                if (AddPlotData(hits)) {
                    viewModel.Update(UpdatePlot(plotData, "Values"));
                }
            });
        }

        public void Render()
        {
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                viewModel.Render();
            }));
        }

        private PlotModel UpdatePlot(ConcurrentDictionary<String, List<PlotData>> dataPoints,
            String title,
            Dictionary<String, OxyColor> colors = null)
        {
            PlotModel tmp = new PlotModel { Title = title != null ? title : "" };
            LinearAxis xAxis = new LinearAxis();
            xAxis.Position = AxisPosition.Bottom;
            double maxXValue = 0;

            LinearAxis yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Left;
            yAxis.ExtraGridlines = new double[] { 0 };
            tmp.Axes.Add(yAxis);     

            tmp.LegendPosition = LegendPosition.BottomLeft;

            foreach (KeyValuePair<String, List<PlotData>> series in dataPoints) {
                // use a vertical line for hits
                if (series.Value.Count > 0 &&
                    series.Value[0].DataStreamType == DataStreamType.SegmentHits) {
                    int indexCount = series.Value.Count();
                    for (int j = 0; j < indexCount; j++) {
                        LineAnnotation annotation = new LineAnnotation();
                        annotation.Type = LineAnnotationType.Vertical;
                        annotation.X = series.Value[j].X;
                        annotation.Text = series.Value[j].Annotation;
                        annotation.Color = series.Value[j].Annotation == HitType.SegmentInternal.ToString() ?
                            OxyColors.Aqua : OxyColors.DarkRed;
                        tmp.Annotations.Add(annotation);
                    }
                }
                // use a linear graph for all other streams
                else {
                    LineSeries lineSeries = new LineSeries
                    {
                        Title = series.Key
                    };

                    // check if specific colors are set
                    if (colors != null && colors.Count() == dataPoints.Count()) {
                        lineSeries.Color = colors[series.Key];
                    }

                    int indexCount = series.Value.Count();
                    for (int j = 0; j < indexCount; j++) {
                        maxXValue = maxXValue < series.Value[j].X ? series.Value[j].X : maxXValue;
                        lineSeries.Points.Add(new DataPoint(series.Value[j].X, series.Value[j].Y));
                    }

                    lineSeries.Smooth = false;
                    tmp.Series.Add(lineSeries);
                }
            }

            // set graph range by highest value from all data Points
            if (range > 0) {
                xAxis.Minimum = maxXValue - range;
                tmp.Axes.Add(xAxis);
            }

            return tmp;
        }

        private bool AddPlotData(JointData jointData)
        {
            // should the plot show this data stream?
            if (PlotMeasuredVariables.Contains(jointData.DataStreamType)) {
                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    // should the plot show this joint?
                    if (PlotJointTypes.Contains(joint.Key)) {
                        // create the new point
                        PlotData point = new PlotData(jointData.AbsTimestamp / 1000, 
                            joint.Value.Position.Z, jointData.DataStreamType);

                        // add to the plot data
                        // does this series exist? if not create it
                        if (!plotData.ContainsKey(jointData.DataStreamType.ToString()
                            + " - " + joint.Key.ToString())) {
                            plotData.TryAdd(jointData.DataStreamType.ToString()
                            + " - " + joint.Key.ToString(), new List<PlotData>());
                        }
                        plotData[jointData.DataStreamType.ToString()
                            + " - " + joint.Key.ToString()].Add(point);
                    }
                    // if this joint should not be shown delete all points for it
                    else {
                        if (plotData.ContainsKey(jointData.DataStreamType.ToString()
                                                    + " - " + joint.Key.ToString())) {
                            List<PlotData> removed;
                            plotData.TryRemove(jointData.DataStreamType.ToString()
                                + " - " + joint.Key.ToString(), out removed);
                        }
                    }                    
                }
                return true;
            }
            // if the data stream should not be shown delete all points of it
            else {
                if(plotData.Count > 0) {
                    foreach (KeyValuePair<string, List<PlotData>> series in plotData) {
                        if (series.Key.Contains(jointData.DataStreamType.ToString())) {
                            List<PlotData> removed;
                            plotData.TryRemove(series.Key, out removed);
                        }
                    }
                }
                return false;
            }
        }

        private bool AddPlotData(List<SegmentHit> hits)
        {
            // should the plot show this data stream?
            if (PlotMeasuredVariables.Contains(DataStreamType.SegmentHits)) {
                // are there new hits
                if (hits.Count > oldHitsCount) {
                    oldHitsCount = hits.Count;

                    // add the hits
                    List<PlotData> points = new List<PlotData>();
                    foreach (SegmentHit hit in hits) {
                        PlotData point = new PlotData(hit.AbsTimestamp / 1000, hit.HitType.ToString(),
                            DataStreamType.SegmentHits);
                        points.Add(point);
                    }
                    List<PlotData> removed;
                    plotData.TryRemove("Segment hits", out removed);
                    plotData.TryAdd("Segment hits", points);

                    return true;
                }
                else {
                    return false;
                }
            }
            // if the data stream should not be shown delete all points of it
            else {
                List<PlotData> removed;
                plotData.TryRemove("Segment hits", out removed);
                oldHitsCount = 0;
                return false;
            }
        }     

        /// <summary>
        /// Pipeline block to plot joints data.
        /// </summary>
        public ActionBlock<JointData> PlotJointDataBlock
        {
            get => plotJointDataBlock; set => plotJointDataBlock = value;
        }
        /// <summary>
        /// Pipeline block to block segment hits.
        /// </summary>
        public ActionBlock<List<SegmentHit>> PlotHitsBlock
        {
            get => plotHitsBlock; set => plotHitsBlock = value;
        }
        public float Range { get => range; set => range = value; }
        public JointDataPlotView View { get => view; private set => view = value; }
        public List<JointType> PlotJointTypes { get => plotJointTypes;
            set => plotJointTypes = value; }
        public List<DataStreamType> PlotMeasuredVariables { get => plotMeasuredVariables;
            set => plotMeasuredVariables = value; }
    }
}
