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
using System.Windows.Media.Imaging;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// SkeletonSideDisplay displays the tracked skeleton in the sagittal plane. 
    /// Additionally it shows the trajectories of the center of mass, the hands and the knees. 
    /// </summary>
    public class SkeletonSideDisplay
    {
        // width of training area in m
        private float areaWidth = 2.5f;

        private ActionBlock<JointData> skeletonBlock;

        private SkeletonSideView view;
        private SkeletonSideViewModel viewModel;

        private int displayWidth = 1920;
        private int displayHeight = 1080;

        // definition of bones
        private List<Tuple<JointType, JointType>> bones;

        // colors and thicknesses
        private const double TrackedBoneThickness = 16;
        private readonly Color trackedBoneColor = Colors.LightGreen;
        private const double TrackedJointThickness = 8;
        private readonly Color trackedJointColor = Colors.Green;

        private const double InferredBoneThickness = 12;
        private readonly Color inferredBoneColor = Colors.Yellow;
        private const double InferredJointThickness = 8;
        private readonly Color inferredJointColor = Colors.Orange;

        private const double NotTrackedBoneThickness = 12;
        private readonly Color notTrackedBoneColor = Colors.OrangeRed;
        private const double NotTrackedJointThickness = 12;
        private readonly Color notTrackedJointColor = Colors.Red;

        private const double TrajectoryThickness = 16;
        private const double MarkerThickness = 24;
        private const double AxisThickness = 3;

        private readonly Color legendFontColor = Colors.White;
        private const double LegendFontSize = 32;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        protected const float InferredZPositionClamp = 0.1f;

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

        private const int PointsBufferCapacity = 50;

        // hand trajectory
        private bool showHandTrajectory = true;
        private CircularBuffer handPoints = new CircularBuffer(PointsBufferCapacity);
        private Color handTrajectoryColor = Colors.DodgerBlue;

        // body center of mass trajectory
        private bool showBodyCOMTrajectory = true;
        private CircularBuffer bodyCOMPoints = new CircularBuffer(PointsBufferCapacity);
        private Color bodyCOMTrajectoryColor = Colors.White;
        private Color horizontalAxisColor = Colors.LightSlateGray;
        private List<int> bodyCOMY = new List<int>();

        // knee trajectory
        private bool showKneeTrajectory = true;
        private CircularBuffer kneePoints = new CircularBuffer(PointsBufferCapacity);
        private Color kneeTrajectoryColor = Colors.PaleVioletRed;

        // debug foot hip connection
        private bool showFootHipConnection = false;

        // set to true when the view dimensions changed
        private bool dimensionsChanged = false;
        private double lastScale = 0;

        public SkeletonSideDisplay()
        {
            View = new SkeletonSideView();
            ViewModel = (SkeletonSideViewModel)View.DataContext;
            ViewModel.Model = this;

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
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0,
                    displayWidth, displayHeight));

                // convert the joint points to display space
                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                float scale = (float)displayWidth / AreaWidth;
                // origin is 0.5m from both edges
                float originOffsetX = 0.5f * scale;
                float originOffsetY = (float)displayHeight - (0.2f + Properties.Settings.Default.FootSpineBaseOffset) * scale;

                foreach (JointType jointType in joints.Keys) {
                    int x = (int)(originOffsetX + joints[jointType].Position.Z * scale);
                    int y = (int)(originOffsetY - joints[jointType].Position.Y * scale);

                    jointPoints[jointType] = new Point(x, y);
                }

                // convert center of mass
                CameraSpacePoint bodyCOM3D = CalculateBodyCOM(joints);
                int cx = (int)(originOffsetX + bodyCOM3D.Z * scale);
                int cy = (int)(originOffsetY - bodyCOM3D.Y * scale);
                // add y of COM in camera space point to list to calculate 
                // the mean and show a mean horizontal axis 
                bodyCOMY.Add(cy);

                // update point queues
                UpdateBodyCOMPoints(new Point(cx, cy));
                UpdateHandPoints(jointPoints);
                UpdateKneePoints(jointPoints);

                DrawBody(joints, jointPoints, dc);

                if (ShowFootHipConnection) {
                    DrawHorizontalAxis(jointPoints, scale, dc);
                }
                if (ShowHandTrajectory) {
                    DrawHandTrajectory(dc);
                }
                if (ShowBodyCOMTrajectory) {
                    DrawBodyCOMTrajectory(dc);
                }
                if (ShowKneeTrajectory) {
                    DrawKneeTrajectory(dc);
                }

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0,
                    displayWidth, displayHeight));
            }

            ViewModel.UpdateSkeletonImage(tmpImageSource);
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        protected void DrawBody(IReadOnlyDictionary<JointType, Joint> joints,
            IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext)
        {
            // Draw the bones
            foreach (var bone in bones) {
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys) {
                // dafault: if joint is not tracked: red joint
                Brush drawBrush = new SolidColorBrush(notTrackedJointColor);
                double jointThickness = NotTrackedJointThickness;

                TrackingState trackingState = joints[jointType].TrackingState;

                // if joint is inferred: yellow joint
                if (trackingState == TrackingState.Inferred) {
                    drawBrush = new SolidColorBrush(inferredJointColor);
                    jointThickness = InferredJointThickness;
                }
                // if jointis tracked: green joint
                else if (trackingState == TrackingState.Tracked) {
                    drawBrush = new SolidColorBrush(trackedJointColor);
                    jointThickness = TrackedJointThickness;
                }
                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType],
                        jointThickness, jointThickness);
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
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints,
            IDictionary<JointType, Point> jointPoints, JointType jointType0,
            JointType jointType1, DrawingContext drawingContext)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // default pen: one of the brushes is not tracked: red bone
            Pen drawPen = new Pen(new SolidColorBrush(notTrackedBoneColor), NotTrackedBoneThickness);

            // if both joints are tracked: green bone
            if ((joint0.TrackingState == TrackingState.Tracked) &&
                (joint1.TrackingState == TrackingState.Tracked)) {
                drawPen = new Pen(new SolidColorBrush(trackedBoneColor), TrackedBoneThickness);
            }
            // if both joints are inferred or one is inferred an one is tracked: yellow bone
            else if ((joint0.TrackingState == TrackingState.Inferred ||
                joint0.TrackingState == TrackingState.Tracked) && (
                joint1.TrackingState == TrackingState.Inferred ||
                joint1.TrackingState == TrackingState.Tracked)) {
                drawPen = new Pen(new SolidColorBrush(inferredBoneColor), InferredBoneThickness);
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
                    new Rect(0, displayHeight - ClipBoundsThickness,
                    displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(displayWidth - ClipBoundsThickness, 0,
                    ClipBoundsThickness, displayHeight));
            }
        }

        private void DrawHorizontalAxis(IDictionary<JointType, Point> jointPoints, float scale, DrawingContext drawingContext)
        {
            Brush drawBrush = Brushes.Blue;

            // draw a point at cfoot (new origin)
            Point footCenter = new Point();
            footCenter.X = (jointPoints[JointType.FootLeft].X
                + jointPoints[JointType.FootRight].X) / 2;
            footCenter.Y = (jointPoints[JointType.FootLeft].Y
                + jointPoints[JointType.FootRight].Y) / 2
                - Properties.Settings.Default.FootSpineBaseOffset * scale;

            drawingContext.DrawEllipse(drawBrush, null, footCenter, TrajectoryThickness, TrajectoryThickness);

            // draw a line between cankle and spine base
            Pen drawPen = new Pen(Brushes.Blue, 1);
            drawingContext.DrawLine(drawPen, footCenter, jointPoints[JointType.SpineBase]);
        }

        private void UpdateBodyCOMPoints(Point bodyCOM)
        {
            bodyCOMPoints.Enqueue(bodyCOM);
        }

        private CameraSpacePoint CalculateBodyCOM(IReadOnlyDictionary<JointType, Joint> joints)
        {
            // calculate 3D center of mass
            // calculate segments
            CameraSpacePoint footCOMRight = new CameraSpacePoint();
            footCOMRight.X = joints[JointType.AnkleRight].Position.X
                + (joints[JointType.FootRight].Position.X
                - joints[JointType.AnkleRight].Position.X)
                * 0.429f;
            footCOMRight.Y = joints[JointType.AnkleRight].Position.Y
                + (joints[JointType.FootRight].Position.Y
                - joints[JointType.AnkleRight].Position.Y)
                * 0.429f;
            footCOMRight.Z = joints[JointType.AnkleRight].Position.Z
                + (joints[JointType.FootRight].Position.Z
                - joints[JointType.AnkleRight].Position.Z)
                * 0.429f;

            CameraSpacePoint footCOMLeft = new CameraSpacePoint();
            footCOMLeft.X = joints[JointType.AnkleLeft].Position.X
                + (joints[JointType.FootLeft].Position.X
                - joints[JointType.AnkleLeft].Position.X)
                * 0.429f;
            footCOMLeft.Y = joints[JointType.AnkleLeft].Position.Y
                + (joints[JointType.FootLeft].Position.Y
                - joints[JointType.AnkleLeft].Position.Y)
                * 0.429f;
            footCOMLeft.Z = joints[JointType.AnkleLeft].Position.Z
                + (joints[JointType.FootLeft].Position.Z
                - joints[JointType.AnkleLeft].Position.Z)
                * 0.429f;

            CameraSpacePoint shankCOMRight = new CameraSpacePoint();
            shankCOMRight.X = joints[JointType.KneeRight].Position.X
                + (joints[JointType.AnkleRight].Position.X
                - joints[JointType.KneeRight].Position.X)
                * 0.433f;
            shankCOMRight.Y = joints[JointType.KneeRight].Position.Y
                + (joints[JointType.AnkleRight].Position.Y
                - joints[JointType.KneeRight].Position.Y)
                * 0.433f;
            shankCOMRight.Z = joints[JointType.KneeRight].Position.Z
                + (joints[JointType.AnkleRight].Position.Z
                - joints[JointType.KneeRight].Position.Z)
                * 0.433f;

            CameraSpacePoint shankCOMLeft = new CameraSpacePoint();
            shankCOMLeft.X = joints[JointType.KneeLeft].Position.X
                + (joints[JointType.AnkleLeft].Position.X
                - joints[JointType.KneeLeft].Position.X)
                * 0.433f;
            shankCOMLeft.Y = joints[JointType.KneeLeft].Position.Y
                + (joints[JointType.AnkleLeft].Position.Y
                - joints[JointType.KneeLeft].Position.Y)
                * 0.433f;
            shankCOMLeft.Z = joints[JointType.KneeLeft].Position.Z
                + (joints[JointType.AnkleLeft].Position.Z
                - joints[JointType.KneeLeft].Position.Z)
                * 0.433f;

            CameraSpacePoint thighCOMRight = new CameraSpacePoint();
            thighCOMRight.X = joints[JointType.HipRight].Position.X
                + (joints[JointType.KneeRight].Position.X
                - joints[JointType.HipRight].Position.X)
                * 0.433f;
            thighCOMRight.Y = joints[JointType.HipRight].Position.Y
                + (joints[JointType.KneeRight].Position.Y
                - joints[JointType.HipRight].Position.Y)
                * 0.433f;
            thighCOMRight.Z = joints[JointType.HipRight].Position.Z
                + (joints[JointType.KneeRight].Position.Z
                - joints[JointType.HipRight].Position.Z)
                * 0.433f;

            CameraSpacePoint thighCOMLeft = new CameraSpacePoint();
            thighCOMLeft.X = joints[JointType.HipLeft].Position.X
                + (joints[JointType.KneeLeft].Position.X
                - joints[JointType.HipLeft].Position.X)
                * 0.433f;
            thighCOMLeft.Y = joints[JointType.HipLeft].Position.Y
                + (joints[JointType.KneeLeft].Position.Y
                - joints[JointType.HipLeft].Position.Y)
                * 0.433f;
            thighCOMLeft.Z = joints[JointType.HipLeft].Position.Z
                + (joints[JointType.KneeLeft].Position.Z
                - joints[JointType.HipLeft].Position.Z)
                * 0.433f;

            CameraSpacePoint forearmCOMRight = new CameraSpacePoint();
            forearmCOMRight.X = joints[JointType.ElbowRight].Position.X
                + (joints[JointType.WristRight].Position.X
                - joints[JointType.ElbowRight].Position.X)
                * 0.682f;
            forearmCOMRight.Y = joints[JointType.ElbowRight].Position.Y
                + (joints[JointType.WristRight].Position.Y
                - joints[JointType.ElbowRight].Position.Y)
                * 0.682f;
            forearmCOMRight.Z = joints[JointType.ElbowRight].Position.Z
                + (joints[JointType.WristRight].Position.Z
                - joints[JointType.ElbowRight].Position.Z)
                * 0.682f;

            CameraSpacePoint forearmCOMLeft = new CameraSpacePoint();
            forearmCOMLeft.X = joints[JointType.ElbowLeft].Position.X
                + (joints[JointType.WristLeft].Position.X
                - joints[JointType.ElbowLeft].Position.X)
                * 0.682f;
            forearmCOMLeft.Y = joints[JointType.ElbowLeft].Position.Y
                + (joints[JointType.WristLeft].Position.Y
                - joints[JointType.ElbowLeft].Position.Y)
                * 0.682f;
            forearmCOMLeft.Z = joints[JointType.ElbowLeft].Position.Z
                + (joints[JointType.WristLeft].Position.Z
                - joints[JointType.ElbowLeft].Position.Z)
                * 0.682f;

            CameraSpacePoint upperArmCOMRight = new CameraSpacePoint();
            upperArmCOMRight.X = joints[JointType.ShoulderRight].Position.X
                + (joints[JointType.ElbowRight].Position.X
                - joints[JointType.ShoulderRight].Position.X)
                * 0.436f;
            upperArmCOMRight.Y = joints[JointType.ShoulderRight].Position.Y
                + (joints[JointType.ElbowRight].Position.Y
                - joints[JointType.ShoulderRight].Position.Y)
                * 0.436f;
            upperArmCOMRight.Z = joints[JointType.ShoulderRight].Position.Z
                + (joints[JointType.ElbowRight].Position.Z
                - joints[JointType.ShoulderRight].Position.Z)
                * 0.436f;

            CameraSpacePoint upperArmCOMLeft = new CameraSpacePoint();
            upperArmCOMLeft.X = joints[JointType.ShoulderLeft].Position.X
                + (joints[JointType.ElbowLeft].Position.X
                - joints[JointType.ShoulderLeft].Position.X)
                * 0.436f;
            upperArmCOMLeft.Y = joints[JointType.ShoulderLeft].Position.Y
                + (joints[JointType.ElbowLeft].Position.Y
                - joints[JointType.ShoulderLeft].Position.Y)
                * 0.436f;
            upperArmCOMLeft.Z = joints[JointType.ShoulderLeft].Position.Z
                + (joints[JointType.ElbowLeft].Position.Z
                - joints[JointType.ShoulderLeft].Position.Z)
                * 0.436f;

            CameraSpacePoint trunkCOM = new CameraSpacePoint();
            trunkCOM.X = joints[JointType.SpineBase].Position.X
                + (joints[JointType.SpineShoulder].Position.X
                - joints[JointType.SpineBase].Position.X)
                * 0.54f;
            trunkCOM.Y = joints[JointType.SpineBase].Position.Y
                + (joints[JointType.SpineShoulder].Position.Y
                - joints[JointType.SpineBase].Position.Y)
                * 0.54f;
            trunkCOM.Z = joints[JointType.SpineBase].Position.Z
                + (joints[JointType.SpineShoulder].Position.Z
                - joints[JointType.SpineBase].Position.Z)
                * 0.54f;

            // calculate  body center of mass by adding all segments with their respective weights
            CameraSpacePoint bodyCOM = new CameraSpacePoint();
            bodyCOM.X = footCOMRight.X * 0.019f + footCOMLeft.X * 0.019f
                + shankCOMRight.X * 0.044f + shankCOMLeft.X * 0.044f
                + thighCOMRight.X * 0.115f + thighCOMLeft.X * 0.115f
                + forearmCOMRight.X * 0.025f + forearmCOMLeft.X * 0.025f
                + upperArmCOMRight.X * 0.031f + upperArmCOMLeft.X * 0.031f
                + trunkCOM.X * 0.532f;
            bodyCOM.Y = footCOMRight.Y * 0.019f + footCOMLeft.Y * 0.019f
                + shankCOMRight.Y * 0.044f + shankCOMLeft.Y * 0.044f
                + thighCOMRight.Y * 0.115f + thighCOMLeft.Y * 0.115f
                + forearmCOMRight.Y * 0.025f + forearmCOMLeft.Y * 0.025f
                + upperArmCOMRight.Y * 0.031f + upperArmCOMLeft.Y * 0.031f
                + trunkCOM.Y * 0.532f;
            bodyCOM.Z = footCOMRight.Z * 0.019f + footCOMLeft.Z * 0.019f
                + shankCOMRight.Z * 0.044f + shankCOMLeft.Z * 0.044f
                + thighCOMRight.Z * 0.115f + thighCOMLeft.Z * 0.115f
                + forearmCOMRight.Z * 0.025f + forearmCOMLeft.Z * 0.025f
                + upperArmCOMRight.Z * 0.031f + upperArmCOMLeft.Z * 0.031f
                + trunkCOM.Z * 0.532f;

            return bodyCOM;
        }

        private void DrawBodyCOMTrajectory(DrawingContext drawingContext)
        {
            // draw the mean horizontal axis
            drawingContext.DrawLine(new Pen(new SolidColorBrush(horizontalAxisColor), AxisThickness),
                new Point(0, bodyCOMY.Average()), new Point(displayWidth, bodyCOMY.Average()));

            // draw trajectory and current point
            Point[] points = Array.ConvertAll(bodyCOMPoints.ToArray(), x => (Point)x);
            DrawTrajectory(points, drawingContext, bodyCOMTrajectoryColor, true);

            // draw the legend
            //DrawLegend(0, "Body center of mass trajectory", drawingContext, bodyCOMTrajectoryColor);
        }

        private void UpdateHandPoints(IDictionary<JointType, Point> jointPoints)
        {
            Point handPoint = new Point();
            handPoint.X = (jointPoints[JointType.HandRight].X
                + jointPoints[JointType.HandLeft].X) / 2;
            handPoint.Y = (jointPoints[JointType.HandRight].Y
                + jointPoints[JointType.HandLeft].Y) / 2;
            handPoints.Enqueue(handPoint);
        }

        private void UpdateKneePoints(IDictionary<JointType, Point> jointPoints)
        {
            Point kneePoint = new Point();
            kneePoint.X = (jointPoints[JointType.KneeRight].X
                + jointPoints[JointType.KneeLeft].X) / 2;
            kneePoint.Y = (jointPoints[JointType.KneeRight].Y
                + jointPoints[JointType.KneeLeft].Y) / 2;
            kneePoints.Enqueue(kneePoint);
        }

        private void DrawHandTrajectory(DrawingContext drawingContext)
        {
            // convert an array of objects to an array of points
            Point[] points = Array.ConvertAll(handPoints.ToArray(), x => (Point)x);
            DrawTrajectory(points, drawingContext, handTrajectoryColor, false);
            //DrawLegend(1, "Handle trajectory", drawingContext, handTrajectoryColor);
        }

        private void DrawKneeTrajectory(DrawingContext drawingContext)
        {
            // convert an array of objects to an array of points
            Point[] points = Array.ConvertAll(kneePoints.ToArray(), x => (Point)x);
            DrawTrajectory(points, drawingContext, kneeTrajectoryColor, false);
            //DrawLegend(2, "Knee trajectory", drawingContext, kneeTrajectoryColor);
        }

        private void DrawTrajectory(Point[] points, DrawingContext drawingContext,
            Color color, bool markCurrentPoint)
        {
            int i = 0;
            foreach (Point point in points) {
                if (i > 0) {
                    Brush drawBrush = new SolidColorBrush()
                    {
                        Color = color,
                        Opacity = 1.0 / PointsBufferCapacity * i
                    };
                    Pen drawPen = new Pen(drawBrush, TrajectoryThickness);

                    drawingContext.DrawLine(drawPen, points[i - 1], point);
                }
                i++;
            }
            if (markCurrentPoint) {
                drawingContext.DrawEllipse(new SolidColorBrush(color), null,
                        points.Last(), MarkerThickness, MarkerThickness);
            }
        }

        private void DrawLegend(int position, string name, DrawingContext drawingContext,
            Color color)
        {
            Pen drawPen = new Pen(new SolidColorBrush() { Color = color }, TrajectoryThickness);

            drawingContext.DrawLine(drawPen,
                new Point(4, 12 + (1 + position) * ((LegendFontSize * 2 + 4) / 2)),
                new Point(20, 12 + (1 + position) * ((LegendFontSize * 2 + 4) / 2)));

            FormattedText text = new FormattedText(
                name,
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                LegendFontSize,
                new SolidColorBrush(legendFontColor));
            drawingContext.DrawText(text, new Point(24, position * (LegendFontSize / 2) + 12));
        }

        public ActionBlock<JointData> SkeletonBlock
        {
            get => skeletonBlock; set => skeletonBlock = value;
        }

        public SkeletonSideView View { get => view; set => view = value; }
        internal SkeletonSideViewModel ViewModel { get => viewModel; set => viewModel = value; }
        public float AreaWidth { get => areaWidth; set => areaWidth = value; }
        public bool ShowHandTrajectory { get => showHandTrajectory; set => showHandTrajectory = value; }
        public bool ShowBodyCOMTrajectory { get => showBodyCOMTrajectory; set => showBodyCOMTrajectory = value; }
        public bool ShowFootHipConnection { get => showFootHipConnection; set => showFootHipConnection = value; }
        public bool ShowKneeTrajectory { get => showKneeTrajectory; set => showKneeTrajectory = value; }
    }
}
