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

        // const
        private const double yMax = 2.0;
        private const double yMin = -2.0;

        private bool newSegmentDetected = false;
        //private List<SegmentHit> hits;
        private long[] detectedBounds;

        // data for peak detection
        private MeanWindowPeakDetector legsPeakDetector;
        private MeanWindowPeakDetector trunkPeakDetector;
        private MeanWindowPeakDetector armsPeakDetector;


        /// <summary>
        /// Creates a new Kleshnev plot with a plot for the last complete segment and an realtime plot of the current Kleshnev velocities.
        /// </summary>
        /// <param name="range">Sets the time range in seconds to display in the realtime plot.</param>
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

            legsPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);
            trunkPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);
            armsPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);

            KleshnevDataBlock = new ActionBlock<KleshnevData>(kleshnevData =>
            {
                PreparePlotData(kleshnevData);
                Update();
            });

            PlotHitsBlock = new ActionBlock<List<SegmentHit>>(hits =>
            {
                // check if a new segment was detected
                long[] segmentBounds = SegmentHitHandler.GetLastSegmentStartEnd(hits);
                if (SegmentHitHandler.IsSegmentValid(hits, segmentBounds) 
                && (detectedBounds == null || (detectedBounds[0] != segmentBounds[0]
                && detectedBounds[1] != segmentBounds[1]))) {
                    detectedBounds = segmentBounds;
                    newSegmentDetected = true;
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

        /// <summary>
        /// Update the plots. Decide if the last segment plot should be updated too.
        /// </summary>
        private void Update()
        {
            // update last segment plot if new hits were detected
            if (newSegmentDetected) {
                Dictionary<string, List<PlotData>> lastSegmentPlotData =
                    new Dictionary<string, List<PlotData>>();

                // filter the plot data between the bounds
                foreach (KeyValuePair<string, List<PlotData>> plotSeries in
                    currentSegmentPlotData) {
                    List<PlotData> plotData = new List<PlotData>();

                    // override peaks with the improved peaks 
                    if (plotSeries.Key == "Legs peaks") {
                        PlotData point = new PlotData(legsPeakDetector.SegmentEnded() / 1000, 
                            "Legs peak", DataStreamType.KleshnevPeak);
                        plotData.Add(point);
                    }
                    else if (plotSeries.Key == "Trunk peaks") {
                        PlotData point = new PlotData(trunkPeakDetector.SegmentEnded() / 1000, 
                            "Trunk peaks", DataStreamType.KleshnevPeak);
                        plotData.Add(point);
                    }
                    else if (plotSeries.Key == "Arms peaks") {
                        PlotData point = new PlotData(armsPeakDetector.SegmentEnded() / 1000,
                            "Arms peaks", DataStreamType.KleshnevPeak);
                        plotData.Add(point);
                    }
                    else {
                        foreach (PlotData point in plotSeries.Value) {
                            if (point.Index >= detectedBounds[0] && point.Index <= detectedBounds[1]) {
                                plotData.Add(point);
                            }
                        }                        
                    }

                    lastSegmentPlotData.Add(plotSeries.Key, plotData);
                }

                viewModel?.UpdateLastSegmentPlot(UpdatePlot(lastSegmentPlotData,
                    Properties.Resources.KleshnevPlotLastSegmentTitle, .0f));

                newSegmentDetected = false;
            }

            // update the continuus plot
            viewModel?.UpdateCurrentSegmentPlot(UpdatePlot(currentSegmentPlotData,
                Properties.Resources.KleshnevPlotCurrentSegmentTitle, Range));
        }

        /// <summary>
        /// Return a PlotModel from given plot data.
        /// </summary>
        /// <param name="dataPoints"></param>
        /// <param name="title"></param>
        /// <param name="valueRange"></param>
        /// <returns></returns>
        private PlotModel UpdatePlot(Dictionary<String, List<PlotData>> dataPoints,
            String title, float valueRange)
        {
            PlotModel tmp = new PlotModel { Title = title != null ? title : "" };
            LinearAxis xAxis = new LinearAxis();
            xAxis.Position = AxisPosition.Bottom;
            double maxXValue = 0;

            LinearAxis yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Left;
            yAxis.ExtraGridlines = new double[] { 0 };

            tmp.LegendPosition = LegendPosition.BottomLeft;

            foreach (KeyValuePair<String, List<PlotData>> series in dataPoints) {
                if (series.Value.Count > 0
                    && series.Value[0].DataStreamType == DataStreamType.KleshnevPeak) {
                    int indexCount = series.Value.Count();
                    for (int j = 0; j < indexCount; j++) {
                        LineAnnotation annotation = new LineAnnotation();
                        annotation.Type = LineAnnotationType.Vertical;
                        annotation.X = series.Value[j].X;
                        annotation.Text = series.Value[j].Annotation;
                        annotation.StrokeThickness = 4;

                        switch (series.Key) {
                            case "Legs peaks":
                                annotation.Color =
                                    kleshnevColors[KleshnevVelocityType.Legs.ToString()];
                                break;
                            case "Trunk peaks":
                                annotation.Color =
                                    kleshnevColors[KleshnevVelocityType.Trunk.ToString()];
                                break;
                            case "Arms peaks":
                                annotation.Color =
                                    kleshnevColors[KleshnevVelocityType.ArmsRight.ToString()];
                                break;
                        }

                        tmp.Annotations.Add(annotation);
                    }
                }
                else if (series.Value.Count > 0) {
                    LineSeries lineSeries = new LineSeries
                    {
                        Title = series.Key
                    };

                    // set the specific colors
                    lineSeries.Color = kleshnevColors[series.Key];

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
            tmp.Axes.Add(yAxis);

            return tmp;
        }

        /// <summary>
        /// Convert Kleshnev data in plot data.
        /// </summary>
        /// <param name="kleshnevData"></param>
        private void PreparePlotData(KleshnevData kleshnevData)
        {
            foreach (KeyValuePair<KleshnevVelocityType, double> klshVel in
                kleshnevData.Velocities) {
                PlotData point = new PlotData(kleshnevData.AbsTimestamp / 1000, klshVel.Value,
                    DataStreamType.KleshnevVelocity, kleshnevData.Index);
                if (currentSegmentPlotData.ContainsKey(klshVel.Key.ToString())) {
                    currentSegmentPlotData[klshVel.Key.ToString()].Add(point);
                }
                else {
                    currentSegmentPlotData.Add(klshVel.Key.ToString(),
                        new List<PlotData>() { point });
                }
            }

            // check for peaks and add them when found
            if (legsPeakDetector.HasPeak(kleshnevData.AbsTimestamp,
                kleshnevData.Velocities[KleshnevVelocityType.Legs])) {
                PlotData point = new PlotData(kleshnevData.AbsTimestamp / 1000, "Legs peak", 
                    DataStreamType.KleshnevPeak, kleshnevData.Index);
                if (currentSegmentPlotData.ContainsKey("Legs peaks")) {
                    currentSegmentPlotData["Legs peaks"].Add(point);
                }
                else {
                    currentSegmentPlotData.Add("Legs peaks",
                        new List<PlotData>() { point });
                }
            }
            if (trunkPeakDetector.HasPeak(kleshnevData.AbsTimestamp,
                kleshnevData.Velocities[KleshnevVelocityType.Trunk])) {
                PlotData point = new PlotData(kleshnevData.AbsTimestamp / 1000, "Trunk peak", 
                    DataStreamType.KleshnevPeak, kleshnevData.Index);
                if (currentSegmentPlotData.ContainsKey("Trunk peaks")) {
                    currentSegmentPlotData["Trunk peaks"].Add(point);
                }
                else {
                    currentSegmentPlotData.Add("Trunk peaks",
                        new List<PlotData>() { point });
                }
            }
            if (armsPeakDetector.HasPeak(kleshnevData.AbsTimestamp,
                (kleshnevData.Velocities[KleshnevVelocityType.ArmsLeft]
                + kleshnevData.Velocities[KleshnevVelocityType.ArmsRight]) / 2)) {
                PlotData point = new PlotData(kleshnevData.AbsTimestamp / 1000, "Arms peak", 
                    DataStreamType.KleshnevPeak, kleshnevData.Index);
                if (currentSegmentPlotData.ContainsKey("Arms peaks")) {
                    currentSegmentPlotData["Arms peaks"].Add(point);
                }
                else {
                    currentSegmentPlotData.Add("Arms peaks",
                        new List<PlotData>() { point });
                }
            }
        }

        public float Range { get => range; set => range = value; }
        public ActionBlock<KleshnevData> KleshnevDataBlock { get => kleshnevDataBlock; set => kleshnevDataBlock = value; }
        public ActionBlock<List<SegmentHit>> PlotHitsBlock { get => plotHitsBlock; set => plotHitsBlock = value; }
        public KleshnevPlotView View { get => view; set => view = value; }
    }
}
