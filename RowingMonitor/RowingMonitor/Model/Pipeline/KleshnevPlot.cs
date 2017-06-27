using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using RowingMonitor.Model.Util;
using RowingMonitor.View;
using RowingMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    class KleshnevPlot
    {
        private ActionBlock<KleshnevData> kleshnevDataBlock;

        private ActionBlock<List<SegmentHit>> plotHitsBlock;

        private float range;

        private Dictionary<String, OxyColor> kleshnevColors;

        // plot view
        private KleshnevPlotView view;
        private KleshnevPlotViewModel viewModel;

        // data for the plot
        private Dictionary<string, List<PlotData>> currentSegmentPlotData =
            new Dictionary<string, List<PlotData>>();

        private bool newSegmentHits = false;
        private List<SegmentHit> hits;

        public KleshnevPlot(float range = 0.0f)
        {
            Range = range;

            View = new KleshnevPlotView();
            viewModel = (KleshnevPlotViewModel)View.DataContext;

            // define colors from Kleshnev´s paper
            kleshnevColors = new Dictionary<string, OxyColor>();
            kleshnevColors.Add(KleshnevVelocityType.ArmsLeft.ToString(), OxyColors.LightGreen);
            kleshnevColors.Add(KleshnevVelocityType.ArmsRight.ToString(), OxyColors.Green);
            kleshnevColors.Add(KleshnevVelocityType.HandleLeft.ToString(), OxyColors.Gray);
            kleshnevColors.Add(KleshnevVelocityType.HandleRight.ToString(), OxyColors.Black);
            kleshnevColors.Add(KleshnevVelocityType.Legs.ToString(), OxyColors.Red);
            kleshnevColors.Add(KleshnevVelocityType.Trunk.ToString(), OxyColors.Blue);

            KleshnevDataBlock = new ActionBlock<KleshnevData>(kleshnevData =>
            {
                PreparePlotData(kleshnevData);
                Update();
            });

            PlotHitsBlock = new ActionBlock<List<SegmentHit>>(newHits =>
            {
                // if new hits occured
                if (hits == null || newHits.Count > hits.Count) {
                    hits = new List<SegmentHit>(newHits);
                    newSegmentHits = true;
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

        private void Update()
        {
            // update last segment plot if new hits were detected
            if (newSegmentHits) {
                long[] segmentBounds = GetLastSegmentStartEnd(hits);
                if (segmentBounds != null) {
                    Dictionary<string, List<PlotData>> lastSegmentPlotData =
                        new Dictionary<string, List<PlotData>>();

                    // filter the plot data between the bounds
                    foreach (KeyValuePair<string, List<PlotData>> plotSeries in
                        currentSegmentPlotData) {
                        List<PlotData> plotData = new List<PlotData>();
                        for (long i = segmentBounds[0]; i <= segmentBounds[1]; i++) {
                            plotData.Add(plotSeries.Value[(int)i]);
                        }
                        lastSegmentPlotData.Add(plotSeries.Key, plotData);
                    }

                    viewModel?.UpdateLastSegmentPlot(UpdatePlot(lastSegmentPlotData,
                        "Kleshnev Velocities Last Segment", .0f));
                }

                newSegmentHits = false;
            }

            // update the continuus plot
            viewModel?.UpdateCurrentSegmentPlot(UpdatePlot(currentSegmentPlotData, 
                "Kleshnev Velocity Current Segment", Range));
        }

        private PlotModel UpdatePlot(Dictionary<String, List<PlotData>> dataPoints,
            String title, float valueRange)
        {
            PlotModel tmp = new PlotModel { Title = title != null ? title : "" };
            LinearAxis xAxis = new LinearAxis();
            xAxis.Position = AxisPosition.Bottom;
            double maxXValue = 0;

            LinearAxis yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Left;
            tmp.Axes.Add(yAxis);

            tmp.LegendPosition = LegendPosition.BottomLeft;

            foreach (KeyValuePair<String, List<PlotData>> series in dataPoints) {
                // use a vertical line for hits
                if (series.Value.Count > 0) {
                    LineSeries lineSeries = new LineSeries
                    {
                        Title = series.Key
                    };

                    // check if specific colors are set
                    if (kleshnevColors != null && kleshnevColors.Count() == dataPoints.Count()) {
                        lineSeries.Color = kleshnevColors[series.Key];
                    }

                    int indexCount = series.Value.Count();
                    for (int j = 0; j < indexCount; j++) {
                        maxXValue = maxXValue < series.Value[j].X ? series.Value[j].X : maxXValue;
                        lineSeries.Points.Add(new DataPoint(series.Value[j].X, series.Value[j].Y));
                    }

                    tmp.Series.Add(lineSeries);
                }
            }

            // set graph range by highest value from all data Points
            if (valueRange > 0) {
                xAxis.Minimum = maxXValue - valueRange;
                tmp.Axes.Add(xAxis);
            }

            return tmp;
        }

        private void PreparePlotData(KleshnevData kleshnevData)
        {
            foreach (KeyValuePair<KleshnevVelocityType, double> klshVel in
                kleshnevData.Velocities) {
                PlotData point = new PlotData();
                point.X = kleshnevData.AbsTimestamp / 1000;
                point.Y = klshVel.Value;
                point.DataStreamType = DataStreamType.Other;
                if (currentSegmentPlotData.ContainsKey(klshVel.Key.ToString())) {
                    currentSegmentPlotData[klshVel.Key.ToString()].Add(point);
                }
                else {
                    currentSegmentPlotData.Add(klshVel.Key.ToString(),
                        new List<PlotData>(){ point });
                }
            }
        }

        private long[] GetLastSegmentStartEnd(List<SegmentHit> hits)
        {
            long[] segmentBounds = { -1, -1 };

            // segments can contain a start hit, an end hit, a end start hit and an internal hit
            // get the index for the last complete segment
            if (hits.Count >= 2) {
                for (int i = hits.Count - 1; i >= 0; i--) {
                    // find last end hit
                    if (segmentBounds[1] == -1 && (hits[i].HitType == HitType.SegmentEnd
                        || hits[i].HitType == HitType.SegmentEndStart)) {
                        segmentBounds[1] = hits[i].Index;
                        continue;
                    }
                    // find last start hit after end hit was found
                    if (segmentBounds[1] != -1 && (hits[i].HitType == HitType.SegmentStart
                        || hits[i].HitType == HitType.SegmentEndStart)) {
                        segmentBounds[0] = hits[i].Index;
                        return segmentBounds;
                    }
                }
            }
            return null;
        }

        public float Range { get => range; set => range = value; }
        public ActionBlock<KleshnevData> KleshnevDataBlock { get => kleshnevDataBlock; set => kleshnevDataBlock = value; }
        public ActionBlock<List<SegmentHit>> PlotHitsBlock { get => plotHitsBlock; set => plotHitsBlock = value; }
        public KleshnevPlotView View { get => view; set => view = value; }        
    }
}
