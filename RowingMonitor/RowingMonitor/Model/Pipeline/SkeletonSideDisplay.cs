using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using RowingMonitor.View;
using RowingMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RowingMonitor.Model.Pipeline
{
    public class SkeletonSideDisplay
    {
        // width of area in m
        private const float areaWidth = 2.5f;

        private ActionBlock<JointData> skeletonBlock;

        private SkeletonSideView view;
        private SkeletonSideViewModel viewModel;

        // definition of bones
        private List<Tuple<JointType, JointType>> bones;

        private Pen bodyColor = new Pen(Brushes.Red, 6);

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        protected const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        protected const double JointThickness = 3;
        protected const double UntrackedJointThickness = 10;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        public SkeletonSideDisplay()
        {
            View = new SkeletonSideView();
            ViewModel = (SkeletonSideViewModel)View.DataContext;

            // a bone defined as a line between two joints
            bones = new List<Tuple<JointType, JointType>>();

            // Torso
            bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            SkeletonBlock = new ActionBlock<JointData>(jointData =>
            {
                UpdateSkeleton(jointData.Joints);
            });
        }

        public void Render()
        {
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                ViewModel.Render();
            }));
        }

        /// <summary>
        /// Updates the view with new data.
        /// </summary>
        public virtual void UpdateSkeleton(IReadOnlyDictionary<JointType, Joint> joints)
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            DrawingImage tmpImageSource = new DrawingImage(drawingGroup);
            using (DrawingContext dc = drawingGroup.Open()) {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.White, null, new Rect(0.0, 0.0,
                    View.ActualWidth, View.ActualHeight));

                //int penIndex = 0;
                Pen drawPen = bodyColor;

                // convert the joint points to display space
                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                float scale = (float)View.ActualWidth / areaWidth;
                // origin is 50cm from both edges
                float originOffsetX = 0.5f * scale;
                float originOffsetY = (float)View.ActualHeight - 0.5f * scale;


                foreach (JointType jointType in joints.Keys) {
                    int x = (int)(originOffsetX + joints[jointType].Position.Z * scale);
                    int y = (int)(originOffsetY - joints[jointType].Position.Y * scale);

                    jointPoints[jointType] = new Point(x, y);
                }

                DrawBody(joints, jointPoints, dc, drawPen);
                DrawHorizontalAxis(jointPoints, dc);

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0,
                    View.ActualWidth, View.ActualHeight));  
            }
            ViewModel.UpdateSkeletonImage(tmpImageSource);
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        protected void DrawBody(IReadOnlyDictionary<JointType, Joint> joints,
            IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext,
            Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in bones) {
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys) {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;
                double jointThickness = UntrackedJointThickness;

                // if joint is tracked make the brush a small red point
                if (trackingState == TrackingState.Tracked) {
                    drawBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
                    jointThickness = JointThickness;
                }
                // if the joint is not fully tracked make the brush a big yellow point
                else if (trackingState == TrackingState.Inferred || trackingState ==
                    TrackingState.NotTracked) {
                    drawBrush = Brushes.Yellow;
                    jointThickness = UntrackedJointThickness;
                }
                // draw the point if the brush is set
                if (drawBrush != null) {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType],
                        jointThickness, jointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints,
            IDictionary<JointType, Point> jointPoints, JointType jointType0,
            JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked) {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = new Pen(Brushes.Gray, 1);
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked)) {
                drawPen = new Pen(Brushes.Red, 6);
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition,
            DrawingContext drawingContext)
        {
            switch (handState) {
                case HandState.Closed:
                    drawingContext.DrawEllipse(handClosedBrush, null, handPosition,
                        HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(handOpenBrush, null, handPosition,
                        HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(handLassoBrush, null, handPosition,
                        HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, View.ActualHeight - ClipBoundsThickness,
                    View.ActualWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, View.ActualWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, View.ActualHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(View.ActualWidth - ClipBoundsThickness, 0,
                    ClipBoundsThickness, View.ActualHeight));
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

        public ActionBlock<JointData> SkeletonBlock
        {
            get => skeletonBlock; set => skeletonBlock = value;
        }

        public SkeletonSideView View { get => view; set => view = value; }
        internal SkeletonSideViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
