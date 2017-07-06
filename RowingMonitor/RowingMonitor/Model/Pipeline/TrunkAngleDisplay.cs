using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using RowingMonitor.View;
using RowingMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media;

namespace RowingMonitor.Model.Pipeline
{
    public class TrunkAngleDisplay
    {
        private ActionBlock<JointData> input;

        private TrunkAngleViewModel viewModel;
        private TrunkAngleView view;

        private double maxCatchAngle;
        private Color maxCatchAngleColor = Colors.Blue;

        private double maxFinishAngle;
        private Color maxFinishAngleColor = Colors.Blue;

        private double currentAngle;
        private Color currentAngleColor = Colors.White;

        private double height;
        private double width;

        public TrunkAngleDisplay()
        {
            View = new TrunkAngleView();
            ViewModel = (TrunkAngleViewModel)View.DataContext;

            Width = 500;
            Height = 250;

            Input = new ActionBlock<JointData>(jointData => {
                UpdateTrunkAngle(jointData);
            });
        }

        public ImageSource GetOutput()
        {
            Height = View.ActualHeight;
            Width = Height * 2;

            DrawingGroup drawingGroup = new DrawingGroup();
            DrawingImage tmpImageSource = new DrawingImage(drawingGroup);
            using (DrawingContext dc = drawingGroup.Open()) {
                         // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0,
                    Width, Height));

                // set the origin
                Point origin = new Point(Width / 2, Height);

                // draw the coordinate system
                Pen coordPen = new Pen(Brushes.Black, 3);
                dc.DrawLine(coordPen, origin, new Point(origin.X, 0));
                dc.DrawLine(coordPen, new Point(0, origin.Y),
                    new Point(Width, origin.Y));

                // draw the vectors of the trunk
                DrawAngle(maxCatchAngle, maxCatchAngleColor, dc, origin);
                DrawAngle(maxFinishAngle, maxFinishAngleColor, dc, origin);
                DrawAngle(currentAngle, currentAngleColor, dc, origin);

                // draw legends
                DrawLegend(2, "Trunk Angle: ", Degrees.RadianToDegree(currentAngle), dc, currentAngleColor);
                DrawLegend(1, "Maximum Catch Angle: ", Degrees.RadianToDegree(maxCatchAngle), dc, maxCatchAngleColor);
                DrawLegend(0, "Maximum Finish Angle: ", Degrees.RadianToDegree(maxFinishAngle), dc, maxFinishAngleColor);
            }
            return tmpImageSource;
        }

        public void Render()
        {            
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                ImageSource tmpImageSource = GetOutput();
                ViewModel.Render(tmpImageSource);
            }));
        }

        private void DrawLegend(int position, string name, double angle,
            DrawingContext drawingContext, Color color)
        {
            Pen drawPen = new Pen(new SolidColorBrush() { Color = color }, 3.0);
            drawingContext.DrawLine(drawPen,
                new Point(2, View.ActualHeight - 8 * (position + 1) - 10 * position),
                new Point(12, View.ActualHeight - 8 * (position + 1) - 10 * position));

            FormattedText text = new FormattedText(
                name + (int)angle + "°",
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                10,
                Brushes.White);
            drawingContext.DrawText(text, new Point(14, View.ActualHeight - 16 * (position + 1)));
        }

        private void DrawAngle(double angle, Color color, DrawingContext drawingContext, Point origin)
        {
            Pen trunkPen = new Pen(new SolidColorBrush(color), 3);
            // start point is in origin
            // calculate endpoint with radius r = Height
            Point endPoint;
            if (angle >= 0) {
                double u = Math.Cos(Math.PI / 2 - angle) * Height;
                double v = Math.Sin(Math.PI / 2 - angle) * Height;
                endPoint = new Point(origin.X + u, Height - v);
            }
            else {
                double u = Math.Cos(Math.Abs(angle)) * Height;
                double v = Math.Sin(Math.Abs(angle)) * Height;
                endPoint = new Point(origin.X - v, Height - u);
            }

            drawingContext.DrawLine(trunkPen, origin, endPoint);
        }

        private void UpdateTrunkAngle(JointData jointData)
        {
            // SpineBase is the new origin
            Point origin = new Point();
            origin.X = jointData.Joints[JointType.SpineBase].Position.Z;
            origin.Y = jointData.Joints[JointType.SpineBase].Position.Y;

            // calculate vector shoulder from SpineShoulder with the new origin
            Point shoulder = new Point();
            shoulder.X = jointData.Joints[JointType.SpineShoulder].Position.Z - origin.X;
            shoulder.Y = jointData.Joints[JointType.SpineShoulder].Position.Y - origin.Y;

            // calculate angle between shoulder vector and vector (0, 1)
            double tmpCurrentAngle = Math.Atan2(shoulder.X, shoulder.Y);
            
            // check if current angle exceeded 0
            if (tmpCurrentAngle * currentAngle < 0) {
                if (tmpCurrentAngle < 0) {
                    maxCatchAngle = 0;
                }
                if (tmpCurrentAngle > 0) {
                    maxFinishAngle = 0;
                }
            }
            if (tmpCurrentAngle < 0) {
                maxCatchAngle = tmpCurrentAngle < maxCatchAngle ? tmpCurrentAngle : maxCatchAngle;
            }
            if (tmpCurrentAngle > 0) {
                maxFinishAngle = tmpCurrentAngle > maxFinishAngle ? tmpCurrentAngle : maxFinishAngle;
            }
            currentAngle = tmpCurrentAngle;
        }

        public ActionBlock<JointData> Input { get => input; set => input = value; }
        public double Height { get => height; set => height = value; }
        public double Width { get => width; set => width = value; }
        public TrunkAngleView View { get => view; set => view = value; }
        internal TrunkAngleViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
