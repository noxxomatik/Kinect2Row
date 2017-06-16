using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;

namespace RowingMonitor.Model.Pipeline
{
    class SideView : FrontalView
    {
        // width of area in m
        private const float areaWidth = 2.5f;

        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SideView(CoordinateMapper mapper, int width, int height) : base(mapper, width, height)
        {

        }

        public override void UpdateSkeleton(IReadOnlyDictionary<JointType, Joint> joints)
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            DrawingImage tmpImageSource = new DrawingImage(drawingGroup);
            using (DrawingContext dc = drawingGroup.Open()) {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.White, null, new Rect(0.0, 0.0, displayWidth, displayHeight));

                //int penIndex = 0;
                Pen drawPen = bodyColors[0];

                // convert the joint points to display space
                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                float scale = displayWidth / areaWidth;
                // origin is 50cm from both edges
                float originOffsetX = 0.5f * scale;
                float originOffsetY = displayHeight - 0.5f * scale;
                

                foreach (JointType jointType in joints.Keys) {
                    int x = (int) (originOffsetX + joints[jointType].Position.Z * scale);
                    int y = (int) (originOffsetY - joints[jointType].Position.Y * scale);

                    jointPoints[jointType] = new Point(x, y);
                }

                DrawBody(joints, jointPoints, dc, drawPen);
                DrawHorizontalAxis(jointPoints, dc);

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
                BodyImageSource = tmpImageSource;
            }
        }

        private void DrawHorizontalAxis(IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext)
        {
            Brush drawBrush = Brushes.Blue;

            // draw a point at cankle (new origin)
            Point ankleCenter = new Point();
            ankleCenter.X = (jointPoints[JointType.AnkleLeft].X
                + jointPoints[JointType.AnkleRight].X) / 2;
            ankleCenter.Y = (jointPoints[JointType.AnkleLeft].Y
                + jointPoints[JointType.AnkleRight].Y) / 2;

            drawingContext.DrawEllipse(drawBrush, null, ankleCenter, JointThickness, JointThickness);

            // draw a line between cankle and spine base
            Pen drawPen = new Pen(Brushes.Blue, 1);
            drawingContext.DrawLine(drawPen, ankleCenter, jointPoints[JointType.SpineBase]);
        }
    }
}
