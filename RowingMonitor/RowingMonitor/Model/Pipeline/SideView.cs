using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;

namespace RowingMonitor.Model
{
    class SideView : FrontalView
    {
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
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, displayWidth, displayHeight));

                //int penIndex = 0;
                Pen drawPen = bodyColors[0];

                // convert the joint points to depth (display) space
                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                foreach (JointType jointType in joints.Keys) {
                    // sometimes the depth(Z) of an inferred joint may show as negative
                    // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                    CameraSpacePoint position = new CameraSpacePoint();
                    // rotate around y-axis (up) for 90°
                    position.X = joints[jointType].Position.Z;
                    position.Y = joints[jointType].Position.Y;
                    // x is too near
                    position.Z = joints[jointType].Position.X + 2;
                    //if (position.X < 0) {
                    //    position.X = InferredZPositionClamp;
                    //}

                    DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(
                        position);

                    //log.Info("X:" + position.X + " Y: " + position.Y + " Z: " + position.Z);
                    //jointPoints[jointType] = new Point(position.Z * 200 + 200, -(position.Y * 200 + 200));
                    jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                }

                DrawBody(joints, jointPoints, dc, drawPen);

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
                BodyImageSource = tmpImageSource;
            }
        }
    }
}
