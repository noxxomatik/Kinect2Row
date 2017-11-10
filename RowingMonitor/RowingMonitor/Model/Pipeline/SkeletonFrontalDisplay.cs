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
    /// <summary>
    /// Adapted visualization of the Body Basics example of the 
    /// Kinect for Windows SDK.
    /// 
    /// SkeletonFrontalDisplay shows the tracked skeleton on top 
    /// of the recorded color image
    /// </summary>
    public class SkeletonFrontalDisplay
    {
        private bool colorImageChanged = false;

        private BitmapSource colorImageSource;

        private ActionBlock<JointData> skeletonBlock;

        private ActionBlock<WriteableBitmap> colorImageBlock;

        private CoordinateMapper coordinateMapper;
        private FrameDescription depthFrameDescription;
        private FrameDescription colorFrameDescription;

        private SkeletonFrontalView view;
        private SkeletonFrontalViewModel viewModel;

        // definition of bones
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        protected const float InferredZPositionClamp = 0.1f;

        // colors and thicknesses
        private const double TrackedBoneThickness = 6;
        private readonly Color trackedBoneColor = Colors.LightGreen;
        private const double TrackedJointThickness = 3;
        private readonly Color trackedJointColor = Colors.Green;

        private const double InferredBoneThickness = 4;
        private readonly Color inferredBoneColor = Colors.Yellow;
        private const double InferredJointThickness = 8;
        private readonly Color inferredJointColor = Colors.Orange;

        private const double NotTrackedBoneThickness = 2;
        private readonly Color notTrackedBoneColor = Colors.OrangeRed;
        private const double NotTrackedJointThickness = 10;
        private readonly Color notTrackedJointColor = Colors.Red;

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

        public void Render()
        {
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                ViewModel.Render();
            }));
        }

        public SkeletonFrontalDisplay(CoordinateMapper mapper, FrameDescription depthFrame,
            FrameDescription colorFrame)
        {
            coordinateMapper = mapper;
            depthFrameDescription = depthFrame;
            colorFrameDescription = colorFrame;

            View = new SkeletonFrontalView();
            ViewModel = (SkeletonFrontalViewModel)View.DataContext;

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

            ColorImageBlock = new ActionBlock<WriteableBitmap>(bitmap =>
            {
                UpdateColorImage(bitmap);
            });
        }

        /// <summary>
        /// Updates the view with new data.
        /// </summary>
        public virtual void UpdateSkeleton(IReadOnlyDictionary<JointType, Joint> joints)
        {
            // calculate width and hight of the skeleton image
            // use the aspect ratio of the kinect image sensor 
            // and convert it 

            DrawingGroup drawingGroup = new DrawingGroup();
            DrawingImage tmpImageSource = new DrawingImage(drawingGroup);
            using (DrawingContext dc = drawingGroup.Open()) {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, 
                    colorFrameDescription.Width, colorFrameDescription.Height));

                // convert the joint points to depth (display) space
                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                foreach (JointType jointType in joints.Keys) {
                    // sometimes the depth(Z) of an inferred joint may show as negative
                    // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                    CameraSpacePoint position = joints[jointType].Position;
                    if (position.Z < 0) {
                        position.Z = InferredZPositionClamp;
                    }

                    ColorSpacePoint depthSpacePoint =
                        coordinateMapper.MapCameraPointToColorSpace(position);
                    jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                }

                DrawBody(joints, jointPoints, dc);

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(
                    new Rect(0.0, 0.0, colorFrameDescription.Width, colorFrameDescription.Height));    
            }
            ViewModel.UpdateSkeletonImage(tmpImageSource.CloneCurrentValue());
        }

        public void UpdateColorImage(WriteableBitmap colorImage)
        {
            ViewModel.UpdateColorImage(colorImage.CloneCurrentValue());
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
                    new Rect(0, colorFrameDescription.Height - ClipBoundsThickness,
                    colorFrameDescription.Width, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, colorFrameDescription.Width, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, colorFrameDescription.Height));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(colorFrameDescription.Width - ClipBoundsThickness, 0,
                    ClipBoundsThickness, colorFrameDescription.Height));
            }
        }

        public ActionBlock<JointData> SkeletonBlock
        {
            get => skeletonBlock; set => skeletonBlock = value;
        }
        public ActionBlock<WriteableBitmap> ColorImageBlock
        {
            get => colorImageBlock; set => colorImageBlock = value;
        }
        public SkeletonFrontalView View { get => view; set => view = value; }
        internal SkeletonFrontalViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
